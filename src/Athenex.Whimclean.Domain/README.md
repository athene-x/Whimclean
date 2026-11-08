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

## Behaviors

`IActivatable` (an `IsActive` flag, in `Athenex.Whimclean.Domain.Behaviors`) marks records an operator can switch on and off. Inactive rows stay visible unless a query opts in with `WhereActive()` from the Application package.

## Persistence concerns are not in this package

Audit stamps (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`) and soft deletion (`IsDeleted`, `DeletedAt`, `DeletedBy`) are shadow properties managed by `AuditableDbContext` in `Athenex.Whimclean.Infrastructure.EntityFrameworkCore`. An entity type opts into them with `HasAuditStamps()` and `HasSoftDelete()` in the EF model configuration, so aggregates declare no persistence fields. An aggregate that needs a value such as `CreatedAt` for a business rule may declare that property itself; the context then stamps the declared property instead of adding a shadow one.

This package contains no EF Core, ASP.NET Core, Identity, PostgreSQL, or multitenancy dependency.

## Text normalization

`TextNormalizer` in `Athenex.Whimclean.Domain.Text` is a static helper. `NormalizeWhitespace` trims and collapses runs of whitespace into single spaces; `NormalizeSlug` additionally lower-cases the value and turns whitespace, hyphens and underscores into single hyphens, for values that end up in URLs or DNS labels.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
