# Athenex.Whimclean.Mediator

[ภาษาไทย](README-TH.md)

Optional integration with [Mediator](https://github.com/martinothamar/Mediator) (the source-generated `martinothamar/Mediator`, not MediatR): a pipeline behavior that commits the unit of work after each command, and a dispatcher that delivers domain events as mediator notifications. The package depends only on `Mediator.Abstractions` (plus `Athenex.Whimclean.Application` and `Athenex.Whimclean.Infrastructure`); the consuming application supplies the source generator.

## Install

```shell
dotnet add package Athenex.Whimclean.Mediator
dotnet add package Mediator.SourceGenerator
```

`Mediator.SourceGenerator` must be referenced by the application project (or the project whose compilation contains the handlers) — the generator emits `AddMediator` and the dispatch code there.

## Register

```csharp
using Athenex.Whimclean.Mediator;

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

`AddMediatorDomainEventDispatcher` registers `MediatorDomainEventDispatcher` as a scoped `IDomainEventDispatcher` with TryAdd semantics, so call it **before** whatever registers the default `ServiceProviderDomainEventDispatcher` (typically the unit-of-work registration). `AddUnitOfWorkBehavior` adds the open generic `UnitOfWorkBehavior<,>` as a scoped `IPipelineBehavior<,>` with TryAddEnumerable, so calling it twice registers the behavior once.

## Commands commit the unit of work

`UnitOfWorkBehavior<TMessage, TResponse>` binds to every message implementing `ICommand<TResponse>` — queries and notifications are untouched. The handler mutates aggregates and returns; the behavior calls `IUnitOfWork.CommitAsync` afterwards, so one command spans exactly one commit and handlers never save:

```csharp
using Athenex.Whimclean.Domain.Results;
using Mediator;

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

Domain events cannot implement `INotification` themselves — the domain layer takes no mediator dependency — so `MediatorDomainEventDispatcher` wraps each event in a `DomainEventNotification` and publishes it through `IPublisher`. Handlers derive from `DomainEventHandler<TEvent>`, which receives every notification and forwards only events of its `TEvent` type:

```csharp
using Athenex.Whimclean.Application.Email;
using Athenex.Whimclean.Mediator;

public sealed class OrderPlacedHandler(IEmailSender emailSender)
    : DomainEventHandler<OrderPlaced>
{
    protected override async ValueTask HandleAsync(
        OrderPlaced domainEvent,
        CancellationToken cancellationToken)
    {
        await emailSender.SendAsync(
            new EmailMessage(
                "member@example.com",
                "Order confirmed",
                $"Order {domainEvent.OrderId} has been placed."),
            cancellationToken);
    }
}
```

The source generator discovers `DomainEventHandler<TEvent>` subclasses like any other `INotificationHandler` and registers them. Events are dispatched after a successful commit, exactly like the built-in pipeline (see the [`Athenex.Whimclean.Infrastructure` README](https://github.com/athene-x/Whimclean/blob/main/src/Athenex.Whimclean.Infrastructure/README.md)). An event with no matching handler is a no-op: the dispatcher swallows the mediator's `MissingMessageHandlerException`. Handler ordering and error aggregation follow the mediator's configured notification publisher; with the default publisher, handlers run sequentially and the first failure propagates.

### Why the notification is not generic

The Mediator source generator does not support generic messages, and it scans every referenced assembly. Versions before 1.2.0 shipped `DomainEventNotification<TEvent>`, which made the generated `Mediator.g.cs` fail to compile (`CS0246: The type or namespace name 'TEvent' could not be found`) in every project that referenced this package and ran the generator. The non-generic notification plus the filtering `DomainEventHandler<TEvent>` base keeps handlers typed without exposing an open generic message. Each handler is invoked once per dispatched event and returns synchronously when the type does not match; the cost is a type check.

Migrating from 1.1.0: replace `INotificationHandler<DomainEventNotification<TEvent>>` with `DomainEventHandler<TEvent>`, rename `Handle` to `HandleAsync`, make it `protected override`, and take `TEvent` directly instead of `notification.DomainEvent`.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
