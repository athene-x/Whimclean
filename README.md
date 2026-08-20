# Athenex.Whimclean

.NET 10 building blocks for domain-driven design and application-layer concerns. The repository produces two packages:

- `Athenex.Whimclean.Domain` provides entities, aggregate roots, domain events, auditing, multitenancy, soft deletion, and operation results.
- `Athenex.Whimclean.Application` provides current-context contracts, email abstractions, pagination models, persistence contracts, and validated query options. It depends on `Athenex.Whimclean.Domain`.

## Installation

```shell
dotnet add package Athenex.Whimclean.Domain --version 0.0.1
dotnet add package Athenex.Whimclean.Application --version 0.0.1
```

## Entities and aggregate roots

```csharp
using Athenex.Whimclean.Domain.Entities;
using Athenex.Whimclean.Domain.Events;

public sealed record OrderCreated(Guid OrderId) : DomainEvent;

public sealed class Order : AggregateRoot<Guid>
{
    public Order(Guid id) : base(id)
    {
        RaiseDomainEvent(new OrderCreated(id));
    }
}
```

Raised events are exposed through `DomainEvents` and can be removed after dispatch by calling `ClearDomainEvents()`.

## Auditing

The actor type includes its own nullability, allowing each application to use an identifier, a name, or a custom actor type.

```csharp
using Athenex.Whimclean.Domain.Auditables;

public sealed class OrderAudit : IAuditable<Guid?>
{
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public Guid? UpdatedBy { get; init; }
}

public sealed class ImportAudit : IAuditable<string?>
{
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}
```

## Results

```csharp
using Athenex.Whimclean.Domain.Results;

Result<Guid> CreateOrder(bool isValid) => isValid
    ? Result<Guid>.Success(Guid.NewGuid())
    : Result<Guid>.Failure(
        Error.Validation("Order.Invalid", "The order is invalid."));
```

Accessing `Value` on a failed result throws `InvalidOperationException`. Check `IsSuccess` or `IsFailure` before reading it.

## Application queries

```csharp
using Athenex.Whimclean.Application.Queries;

var options = new ListQueryOptions(
    searchTerm: "pending",
    sort: [new SortTerm("createdAt", SortDirection.Descending)],
    filters: [new FilterTerm("status", "Pending")],
    page: 1,
    size: 20);
```

Query options validate their limits and expose materialized read-only collections, allowing infrastructure code to translate only approved fields and operators into database queries.

## Pagination

```csharp
using Athenex.Whimclean.Application.Pagination;

var result = new OffsetPagedResult<string>(
    items: ["first", "second"],
    page: 1,
    size: 20,
    totalItems: 2);
```

## Email

Implement `IEmailSender` in the infrastructure layer and consume it from application use cases:

```csharp
var message = new EmailMessage(
    recipient: "member@example.com",
    subject: "Welcome",
    textBody: "Your account is ready.");

await emailSender.SendAsync(message, cancellationToken);
```
