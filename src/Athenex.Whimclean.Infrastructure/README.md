# Athenex.Whimclean.Infrastructure

Provider-neutral infrastructure building blocks used by the Whimclean EF Core integration: audit actor resolution and the in-process domain-event pipeline.

## Install

```shell
dotnet add package Athenex.Whimclean.Infrastructure
```

## Audit actor resolution

`IAuditActorResolver<TUserId, TActor>` converts the authenticated user identifier into the representation stored by audit columns.

For a `Guid` user ID stored in nullable `Guid?` audit properties:

```csharp
using Athenex.Whimclean.Infrastructure;

services.AddNullableAuditActorResolver<Guid>();
```

When the types are identical and non-nullable:

```csharp
services.AddIdentityAuditActorResolver<Guid>();
```

Both helpers use TryAdd semantics. Implement the interface yourself when audit actors require mapping to a name, service account, or value object:

```csharp
services.AddSingleton<
    IAuditActorResolver<Guid, ActorStamp>,
    ActorStampResolver>();
```

## Domain-event pipeline

The package ships an in-process pipeline: `IDomainEventDispatcher` delivers each committed event to every `IDomainEventHandler<TEvent>` registered for its runtime type. The unit of work invokes the dispatcher after a successful commit, so a handler failure never rolls the persisted changes back.

```csharp
using Athenex.Whimclean.Infrastructure;

services.AddDomainEventDispatcher();
services.AddDomainEventHandler<OrderPlaced, SendConfirmationEmail>();
services.AddDomainEventHandler<OrderPlaced, ReserveStock>();
```

`AddDomainEventDispatcher` registers `ServiceProviderDomainEventDispatcher` with TryAdd semantics; `AddDomainEventHandler` uses TryAddEnumerable, so several handlers may react to one event while duplicate registrations collapse to one.

```csharp
public sealed class SendConfirmationEmail(IEmailSender emailSender)
    : IDomainEventHandler<OrderPlaced>
{
    public Task HandleAsync(
        OrderPlaced domainEvent,
        CancellationToken cancellationToken = default) =>
        emailSender.SendOrderConfirmationAsync(
            domainEvent.OrderId,
            cancellationToken);
}
```

### Dispatch failures

When a handler throws after the commit succeeded, the save surfaces a `DomainEventDispatchException`. The save must not be retried — the data is already persisted; the events that never reached their handlers are on `UndeliveredEvents` (the failing event first, then any never attempted), ready to be handled or replayed.

### Bridging to a mediator

The default dispatcher is replaceable: register your own `IDomainEventDispatcher` (before or instead of `AddDomainEventDispatcher` — TryAdd leaves it in place) to bridge into a mediator or message dispatcher:

```csharp
using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Infrastructure.DomainEvents;

public sealed class MediatorDomainEventDispatcher(IMediator mediator)
    : IDomainEventDispatcher
{
    public Task DispatchAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default) =>
        mediator.Publish(domainEvent, cancellationToken);
}
```

```csharp
services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();
```

This package deliberately does not select MediatR, EF Core, a database provider, Identity, or multitenancy.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
