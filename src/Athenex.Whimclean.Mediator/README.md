# Athenex.Whimclean.Mediator

Optional integration with [Mediator](https://github.com/martinothamar/Mediator) (the source-generated `martinothamar/Mediator`, not MediatR): a pipeline behavior that commits the unit of work after each command, and a dispatcher that delivers domain events as mediator notifications. The package depends only on `Mediator.Abstractions`; the consuming application supplies the source generator.

## Install

```shell
dotnet add package Athenex.Whimclean.Mediator
dotnet add package Mediator.SourceGenerator
```

`Mediator.SourceGenerator` must be referenced by the application project (or the project whose compilation contains the handlers) — the generator emits `AddMediator` and the dispatch code there.

## Register

```csharp
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});

builder.Services
    .AddMediatorDomainEventDispatcher() // before any default-dispatcher registration
    .AddUnitOfWorkBehavior();

// e.g. AddPostgreSqlUnitOfWork<AppDbContext>(...) afterwards; its
// TryAdd of the default dispatcher then yields to the mediator one.
```

`AddMediatorDomainEventDispatcher` uses TryAdd semantics, so call it **before** whatever registers the default `ServiceProviderDomainEventDispatcher` (typically the unit-of-work registration).

## Commands commit the unit of work

`UnitOfWorkBehavior` binds to every message implementing `ICommand<TResponse>` — queries and notifications are untouched. The handler mutates aggregates and returns; the behavior calls `IUnitOfWork.CommitAsync` afterwards, so one command spans exactly one commit and handlers never save:

```csharp
public sealed record PlaceOrder(Guid CustomerId) : ICommand<Result<Guid>>;

public sealed class PlaceOrderHandler(AppDbContext context)
    : ICommandHandler<PlaceOrder, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(
        PlaceOrder command, CancellationToken cancellationToken)
    {
        var order = Order.Place(command.CustomerId);
        context.Orders.Add(order);
        return order.Id; // no SaveChanges/Commit here
    }
}
```

The commit is skipped when the handler returns a failed `Result` (or `Result<T>`) or throws, leaving pending changes unpersisted.

## Domain events as notifications

Domain events cannot implement `INotification` themselves — the domain layer takes no mediator dependency — so `MediatorDomainEventDispatcher` wraps each event in `DomainEventNotification<TEvent>` closed over the event's runtime type. Handlers subscribe to the wrapper:

```csharp
public sealed class OrderPlacedHandler
    : INotificationHandler<DomainEventNotification<OrderPlaced>>
{
    public ValueTask Handle(
        DomainEventNotification<OrderPlaced> notification,
        CancellationToken cancellationToken)
    {
        var placed = notification.DomainEvent;
        // react to the event
        return default;
    }
}
```

Events are dispatched after a successful commit, exactly like the built-in pipeline (see the `Athenex.Whimclean.Infrastructure` README). An event without a registered handler is a no-op. Handler ordering and error aggregation follow the mediator's configured notification publisher; with the default publisher, handlers run sequentially and the first failure propagates.
