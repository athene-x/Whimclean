# Upgrading to 2.0.0

[ภาษาไทย](upgrading-to-2.0-TH.md)

Version 2.0.0 is a breaking release with one goal: the `Domain` package carries no persistence concerns. Audit stamps and soft deletion move from domain interfaces into EF Core shadow properties, and the query-string parser moves to the API package. Every registration method from 1.3.0 keeps working; the changes are in the domain model, the EF model configuration, and one namespace.

## Contents

- [Summary of breaking changes](#summary-of-breaking-changes)
- [Domain model](#domain-model)
- [EF Core model configuration](#ef-core-model-configuration)
- [Soft delete](#soft-delete)
- [Reading stamps in queries](#reading-stamps-in-queries)
- [Migrations](#migrations)
- [List query parser](#list-query-parser)
- [Tests](#tests)
- [Checklist](#checklist)

## Summary of breaking changes

| 1.3 | 2.0 |
| --- | --- |
| `AuditableAggregateRoot<TId, TActor>`, `AuditableEntity<TId, TActor>` | Removed. Inherit `AggregateRoot<TId>` / `Entity<TId>` and call `HasAuditStamps()` in the EF configuration. |
| `IAuditable<TActor>`, `ICreateAuditable<TActor>`, `IUpdateAuditable<TActor>` | Removed. Stamping is driven by the model annotation, not by an interface. |
| `ISoftDelete<TActor>` with `IsDeleted`, `DeletedAt`, `DeletedBy` on the entity | Removed. Call `HasSoftDelete()`; the properties become shadow properties. |
| Entity flips `IsDeleted` itself; `Remove` throws for soft-deletable types | `Remove` soft-deletes; `HardDelete` deletes the row; `Restore` reverses. |
| `Athenex.Whimclean.Application.Queries.ListQueryParser` | `Athenex.Whimclean.Api.AspNetCore.Queries.ListQueryParser` (same API). |
| `IActivatable` | Unchanged. |

Nothing else in `Domain`, `Application`, `Mediator`, `Auditing`, `Multitenancy.Abstractions` or the API packages changed shape.

## Domain model

Replace the auditable base classes and drop the audit and soft-delete members from every entity:

```csharp
// 1.3
public sealed class Customer : AuditableAggregateRoot<Guid, Guid?>, ISoftDelete<Guid?>
{
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public void Delete() => IsDeleted = true;
}

// 2.0
public sealed class Customer : AggregateRoot<Guid>
{
}
```

Delete domain methods whose only job was to flip `IsDeleted` (`Delete()`, `Restore()`); the persistence layer owns that state now. Keep methods that encode a business state change (for example `Archive()` setting a `Status`), because archiving is a domain decision and soft deletion is a storage decision.

If a business rule genuinely needs the creation instant (for example "an order cannot be cancelled 24 hours after it was placed"), keep a domain property such as `PlacedAt` that the aggregate sets itself from the `now` the handler passes in. Do not reintroduce `CreatedAt` for that purpose: the stamp is written by infrastructure and is not part of the model.

Domain tests that asserted `CreatedAt`, `CreatedBy` or `IsDeleted` on aggregates are deleted; those behaviors are tested at the EF layer.

## EF Core model configuration

Opt each entity type in from `ConfigureModel`. Nothing happens for types you do not configure.

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(customer => customer.Id);

        builder.HasAuditStamps();   // CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        builder.HasSoftDelete();    // IsDeleted, DeletedAt, DeletedBy + SoftDeleteFilter
    }
}
```

The context adds the properties after `ConfigureModel` runs, using its own `TActor`:

| Property | Type | Added by |
| --- | --- | --- |
| `CreatedAt` | `DateTimeOffset` | `HasAuditStamps()` |
| `CreatedBy` | `TActor`; required for a non-nullable value type such as `Guid`, nullable otherwise (including every reference type such as `string`, because nullable reference annotations are not visible at runtime) | `HasAuditStamps()` |
| `UpdatedAt` | `DateTimeOffset?` | `HasAuditStamps()` |
| `UpdatedBy` | nullable `TActor` | `HasAuditStamps()` |
| `IsDeleted` | `bool` | `HasSoftDelete()` |
| `DeletedAt` | `DateTimeOffset?` | `HasSoftDelete()` |
| `DeletedBy` | nullable `TActor` | `HasSoftDelete()` |

The names are constants on `AuditStampNames` and `SoftDeleteNames`. To change a column name, declare the property first and the context will reuse it:

```csharp
builder.HasAuditStamps();
builder.Property<DateTimeOffset>(AuditStampNames.CreatedAt).HasColumnName("created_at");
```

If an entity still declares a CLR property with the same name and type as a stamp (for example a `CreatedAt` you decided to keep), the context stamps that property instead of adding a shadow one, so existing columns keep working.

## Soft delete

| Operation | 1.3 | 2.0 |
| --- | --- | --- |
| Soft delete | `entity.Delete()` then save | `context.Remove(entity)` (or `repository.Remove`) then save |
| Hard delete | Not possible for `ISoftDelete` types | `context.HardDelete(entity)` then save |
| Restore | `entity.Restore()` then save | `context.Restore(entity)` then save (load with `IncludeSoftDeleted()` first) |
| Read the flag | `entity.IsDeleted` | `context.IsSoftDeleted(entity)` or `EF.Property<bool>(e, SoftDeleteNames.IsDeleted)` |

A soft delete through `Remove` also stamps `UpdatedAt`/`UpdatedBy` when the type has audit stamps, because the row is updated. The named filter `SoftDeleteFilter`, `IncludeSoftDeleted()`, and the rule that a bare `IgnoreQueryFilters()` also disables the tenant filter are unchanged.

Repositories that exposed `RemoveAsync(aggregate)` need no signature change; their body stays `dbContext.Remove(aggregate)`.

Imports that must insert rows already marked deleted set the shadow value after `Add`:

```csharp
context.Customers.Add(customer);
context.Entry(customer).Property(SoftDeleteNames.IsDeleted).CurrentValue = true;
```

## Reading stamps in queries

Projections that read `customer.CreatedAt` now read the shadow property:

```csharp
// 1.3
.Select(customer => new CustomerResponse(customer.Id, customer.Name, customer.CreatedAt))
.OrderByDescending(customer => customer.CreatedAt)

// 2.0
.Select(customer => new CustomerResponse(
    customer.Id,
    customer.Name,
    EF.Property<DateTimeOffset>(customer, AuditStampNames.CreatedAt)))
.OrderByDescending(customer => EF.Property<DateTimeOffset>(customer, AuditStampNames.CreatedAt))
```

`SortFields<T>` entries are expressions, so a sortable `createdAt` becomes:

```csharp
{ "createdAt", customer => EF.Property<DateTimeOffset>(customer, AuditStampNames.CreatedAt) }
```

EF Core translates `EF.Property` in `Select`, `Where`, `OrderBy` and `SortFields` alike; nothing changes in the generated SQL.

## Migrations

Column names and types are unchanged for entities that already had the 1.3 properties, so `dotnet ef migrations add` after the upgrade should produce an empty migration for them. Review the generated migration before committing:

- A non-empty migration usually means a property was renamed or its nullability changed; compare against the table above.
- Entities that newly opt into `HasSoftDelete()` gain three columns; `IsDeleted` is `NOT NULL` and existing rows need a default of `false` (add `.HasDefaultValue(false)` on the property in `ConfigureModel`, or set it in the migration).

## List query parser

Change the namespace import:

```csharp
// 1.3
using Athenex.Whimclean.Application.Queries;   // ListQueryParser

// 2.0
using Athenex.Whimclean.Api.AspNetCore.Queries; // ListQueryParser
using Athenex.Whimclean.Application.Queries;    // ListQueryOptions, FilterTerm, SortTerm, EnumFilter
```

`ListQueryRequest.ToListQueryOptions()` and `ListQueryRequestValidator<TRequest>` are unaffected. Application-layer code that parsed query strings itself should move that call to the API project; the Application package no longer references query-string syntax.

## Tests

- Domain tests: delete assertions on audit and soft-delete members.
- Application tests with in-memory fakes: fakes that tracked `IsDeleted` on aggregates drop that state; a fake repository's `Remove` simply removes the item from its list.
- EF tests: read stamps through `context.Entry(entity).Property(AuditStampNames.CreatedAt).CurrentValue`, soft-delete through `context.Remove` + `IsSoftDeleted`, and restore through `context.Restore`.

## Checklist

1. Update every `Athenex.Whimclean.*` package reference to `2.0.0`.
2. Replace `AuditableAggregateRoot`/`AuditableEntity` with `AggregateRoot`/`Entity`; remove `IAuditable`, `ISoftDelete` and their members from entities.
3. Add `HasAuditStamps()` / `HasSoftDelete()` to each affected `IEntityTypeConfiguration`.
4. Replace `entity.Delete()`/`entity.Restore()` call sites with `Remove`/`Restore` on the context or repository.
5. Replace `.CreatedAt`, `.UpdatedAt`, `.IsDeleted` reads in queries with `EF.Property<T>(…)`.
6. Change `ListQueryParser` imports to `Athenex.Whimclean.Api.AspNetCore.Queries`.
7. Build, run `dotnet ef migrations add Upgrade20 --dry-run` style review (expect no column changes), run the test suites.
