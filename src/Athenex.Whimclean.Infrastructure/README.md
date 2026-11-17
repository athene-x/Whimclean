# Athenex.Whimclean.Infrastructure

[ภาษาไทย](README-TH.md)

Provider-neutral infrastructure building blocks used by the Whimclean EF Core integration: audit actor resolution, the in-process domain-event pipeline, and deterministic seed identifiers.

## Install

```shell
dotnet add package Athenex.Whimclean.Infrastructure
```

## Audit actor resolution

`IAuditActorResolver<TUserId, TActor>` (in `Athenex.Whimclean.Infrastructure.Auditing`) converts the authenticated user identifier into the representation stored by audit columns.

For a `Guid` user ID stored in nullable `Guid?` audit properties:

```csharp
using Athenex.Whimclean.Infrastructure;

services.AddNullableAuditActorResolver<Guid>();
```

When the types are identical and non-nullable:

```csharp
services.AddIdentityAuditActorResolver<Guid>();
```

Both helpers register a singleton with TryAdd semantics (`NullableAuditActorResolver<TUserId>` requires a value type; `IdentityAuditActorResolver<TActor>` returns the id unchanged). Implement the interface yourself when audit actors require mapping to a name, service account, or value object:

```csharp
using Athenex.Whimclean.Infrastructure.Auditing;

services.AddSingleton<
    IAuditActorResolver<Guid, ActorStamp>,
    ActorStampResolver>();
```

## Domain-event pipeline

The package ships an in-process pipeline in `Athenex.Whimclean.Infrastructure.DomainEvents`: `IDomainEventDispatcher` delivers each committed event to every `IDomainEventHandler<TEvent>` registered for its runtime type. The unit of work invokes the dispatcher after a successful commit, so a handler failure never rolls the persisted changes back.

```csharp
using Athenex.Whimclean.Infrastructure;

services.AddDomainEventDispatcher();
services.AddDomainEventHandler<OrderPlaced, SendConfirmationEmail>();
services.AddDomainEventHandler<OrderPlaced, ReserveStock>();
```

`AddDomainEventDispatcher` registers `ServiceProviderDomainEventDispatcher` as a scoped service with TryAdd semantics; `AddDomainEventHandler` registers the handler as scoped with TryAddEnumerable, so several handlers may react to one event while duplicate registrations collapse to one.

```csharp
using Athenex.Whimclean.Application.Email;
using Athenex.Whimclean.Infrastructure.DomainEvents;

public sealed class SendConfirmationEmail(IEmailSender emailSender)
    : IDomainEventHandler<OrderPlaced>
{
    public Task HandleAsync(
        OrderPlaced domainEvent,
        CancellationToken cancellationToken = default) =>
        emailSender.SendAsync(
            new EmailMessage(
                "member@example.com",
                "Order confirmed",
                $"Order {domainEvent.OrderId} has been placed."),
            cancellationToken);
}
```

### Dispatch failures

When a handler throws after the commit succeeded, the save surfaces a `DomainEventDispatchException`. The save must not be retried — the data is already persisted; the events that never reached their handlers are on `UndeliveredEvents` (the failing event first, then any never attempted), ready to be handled or replayed. The EF Core unit of work clears each aggregate's events before dispatching, so events raised by a handler during dispatch survive for the next commit, and it dispatches with `CancellationToken.None` so a cancelled request cannot abort delivery for changes that are already persisted.

### Bridging to a mediator

The default dispatcher is replaceable: register your own `IDomainEventDispatcher` (before or instead of `AddDomainEventDispatcher` — TryAdd leaves it in place) to bridge into a mediator or message dispatcher. The example below targets a MediatR-style `IMediator` whose `Publish` accepts any object:

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

For the source-generated `martinothamar/Mediator`, the `Athenex.Whimclean.Mediator` package already ships this bridge as `AddMediatorDomainEventDispatcher()`; see [its README](https://github.com/athene-x/Whimclean/blob/main/src/Athenex.Whimclean.Mediator/README.md).

This package deliberately does not select MediatR, EF Core, a database provider, Identity, or multitenancy.

## Deterministic seed identifiers

`DeterministicGuid.Create(parentId, key)` (in `Athenex.Whimclean.Infrastructure.Seeding`) derives a stable identifier from a parent id and a key by hashing them with SHA-256, so seeded rows such as system roles get the same id in every environment and seeders stay idempotent. The `Create(key)` overload uses `Guid.Empty` as the parent for top-level seed rows:

```csharp
using Athenex.Whimclean.Infrastructure.Seeding;

var ownerRoleId = DeterministicGuid.Create(tenantId, "owner");
```

Repository and full architecture guide: https://github.com/athene-x/Whimclean
