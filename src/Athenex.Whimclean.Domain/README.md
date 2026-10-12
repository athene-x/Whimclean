# Athenex.Whimclean.Domain

[ภาษาไทย](README-TH.md)

Small, dependency-free domain building blocks for .NET 10 applications.

## Install

```shell
dotnet add package Athenex.Whimclean.Domain
```

## Aggregate roots and domain events

Derive aggregate roots from `AggregateRoot<TId>` and raise events inside domain behavior:

```csharp
using Athenex.Whimclean.Domain.Entities;
using Athenex.Whimclean.Domain.Events;

public sealed record OrderSubmitted(Guid OrderId) : DomainEvent;

public sealed class Order : AggregateRoot<Guid>
{
    public Order(Guid id) : base(id)
    {
    }

    public void Submit()
    {
        RaiseDomainEvent(new OrderSubmitted(Id));
    }
}
```

`Entity<TId>` rejects a default-valued id in its constructor and compares entities by runtime type and `Id`; two entities whose id is still the default are never equal. `DomainEvent` is an abstract record that gives every event a version 7 `EventId`.

Infrastructure reads `DomainEvents` and calls `ClearDomainEvents()` after successful dispatch. `ClearDomainEvents()` is implemented explicitly on `IHasDomainEvents`, so it is reachable through that interface and stays off the aggregate's public surface.

## Operation results

Use `Result` or `Result<TValue>` for expected domain and application failures:

```csharp
using Athenex.Whimclean.Domain.Results;

Result<Guid> CreateOrder(bool valid) => valid
    ? Result<Guid>.Success(Guid.NewGuid())
    : Result<Guid>.Failure(
        Error.Validation("Order.Invalid", "The order is invalid."));
```

Available error factories are `Error.Failure`, `Error.Validation`, `Error.NotFound`, `Error.Conflict`, `Error.Unauthorized` and `Error.Forbidden`; each sets the matching `ErrorType`. Check `IsSuccess` or `IsFailure` before reading `Result<TValue>.Value`, which throws on a failed result. Values and errors convert implicitly to results, and `Match`, `Map` and `Bind` chain results without unwrapping them.

## Auditing and behaviors

Implement only the contracts the entity needs:

```csharp
using Athenex.Whimclean.Domain.Auditables;
using Athenex.Whimclean.Domain.Behaviors;

public sealed class OrderAudit : IAuditable<Guid?>, ISoftDelete<Guid?>
{
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
```

The contracts are read-only; the EF infrastructure stamps the values at save time. `IAuditable<TActor>` combines `ICreateAuditable<TActor>` and `IUpdateAuditable<TActor>`, which may be implemented separately. The actor type carries its own nullability, so applications may store `Guid?`, `string?`, or a custom actor value. `IActivatable` (an `IsActive` flag) is the third behavior; inactive rows stay visible unless a query opts in with `WhereActive()` from the Application package.

This package contains no EF Core, ASP.NET Core, Identity, PostgreSQL, or multitenancy dependency.

## Auditable base classes

`AuditableEntity<TId, TActor>` and `AuditableAggregateRoot<TId, TActor>` declare the `CreatedAt`, `CreatedBy`, `UpdatedAt` and `UpdatedBy` properties that the EF infrastructure stamps, so entity types do not repeat them:

```csharp
using Athenex.Whimclean.Domain.Entities;

public sealed class Tenant : AuditableAggregateRoot<Guid, Guid?>
{
    private Tenant(Guid id) : base(id) { }
}
```

## Text normalization

`TextNormalizer` in `Athenex.Whimclean.Domain.Text` is a static helper. `NormalizeWhitespace` trims and collapses runs of whitespace into single spaces; `NormalizeSlug` additionally lower-cases the value and turns whitespace, hyphens and underscores into single hyphens, for values that end up in URLs or DNS labels.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
