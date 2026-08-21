# Athenex.Whimclean.Infrastructure

Provider-neutral infrastructure contracts used by the Whimclean EF Core integration.

## Install

```shell
dotnet add package Athenex.Whimclean.Infrastructure
```

## Audit actor resolution

`IAuditActorResolver<TUserId, TActor>` converts the authenticated user identifier into the representation stored by audit columns.

For a `Guid` user ID stored in nullable `Guid?` audit properties:

```csharp
using Athenex.Whimclean.Infrastructure.Auditing;

services.AddSingleton<
    IAuditActorResolver<Guid, Guid?>,
    NullableAuditActorResolver<Guid>>();
```

When the types are identical and non-nullable:

```csharp
services.AddSingleton<
    IAuditActorResolver<Guid, Guid>,
    IdentityAuditActorResolver<Guid>>();
```

Implement the interface yourself when audit actors require mapping to a name, service account, or value object.

## Domain-event dispatch boundary

Implement `IDomainEventDispatcher` in the consuming infrastructure layer and bridge it to the chosen mediator or message dispatcher:

```csharp
using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Infrastructure.DomainEvents;

public sealed class DomainEventDispatcher(IMediator mediator)
    : IDomainEventDispatcher
{
    public Task DispatchAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default) =>
        mediator.Publish(domainEvent, cancellationToken);
}
```

```csharp
services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
```

This package deliberately does not select MediatR, EF Core, a database provider, Identity, or multitenancy.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
