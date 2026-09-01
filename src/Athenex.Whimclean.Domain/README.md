# Athenex.Whimclean.Domain

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

Infrastructure can read `DomainEvents` and call `ClearDomainEvents()` after successful dispatch.

## Operation results

Use `Result` or `Result<TValue>` for expected domain and application failures:

```csharp
using Athenex.Whimclean.Domain.Results;

Result<Guid> CreateOrder(bool valid) => valid
    ? Result<Guid>.Success(Guid.NewGuid())
    : Result<Guid>.Failure(
        Error.Validation("Order.Invalid", "The order is invalid."));
```

Available error types are `Failure`, `Validation`, `NotFound`, and `Conflict`. Check `IsSuccess` or `IsFailure` before reading `Result<TValue>.Value`.

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

The actor type carries its own nullability, so applications may store `Guid?`, `string?`, or a custom actor value.

This package contains no EF Core, ASP.NET Core, Identity, PostgreSQL, or multitenancy dependency.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
