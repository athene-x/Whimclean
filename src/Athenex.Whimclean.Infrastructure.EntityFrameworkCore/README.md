# Athenex.Whimclean.Infrastructure.EntityFrameworkCore

[ภาษาไทย](README-TH.md)

Provider-neutral EF Core persistence for audit stamping, soft deletion, concurrency translation, domain-event dispatch, and unit-of-work registration. Audit and soft-delete properties are shadow properties, so the domain model declares none of them.

## Install

```shell
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer # example provider
```

The package references `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational`; add a provider yourself. Use the PostgreSQL adapter instead of referencing Npgsql directly when PostgreSQL is the target database.

## Create the business context

```csharp
using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Infrastructure.Auditing;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    TimeProvider timeProvider,
    ICurrentUser<Guid> currentUser,
    IAuditActorResolver<Guid, Guid?> auditActorResolver,
    IDomainEventDispatcher domainEventDispatcher)
    : AuditableDbContext<Guid, Guid?>(
        options,
        timeProvider,
        currentUser,
        auditActorResolver,
        domainEventDispatcher)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasAuditStamps();
            builder.HasSoftDelete();
        });
    }
}
```

`TUserId` is the trusted current-user identifier. `TActor` is the value stored in the actor columns and may be nullable. `OnModelCreating` is sealed: it runs `ConfigureModel` first, then adds the audit-stamp and soft-delete properties with the soft-delete query filters, then `ConfigureAdditionalInfrastructureModel` (the hook the multitenancy context uses). Override `ValidateApplicationWrites` to inspect tracked changes before audit stamping and throw to abort the commit.

## Register with any EF Core provider

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore;

services.AddSingleton(TimeProvider.System);
services.AddScoped<ICurrentUser<Guid>, HttpCurrentUser>();
services.AddNullableAuditActorResolver<Guid>();
services.AddDomainEventDispatcher();

services.AddEntityFrameworkCoreUnitOfWork<ApplicationDbContext>((_, options) =>
    options.UseSqlServer(connectionString));
```

`AddEntityFrameworkCoreUnitOfWork<TContext>` requires `IUnitOfWork` and registers the scoped context as that interface. Use `AddEntityFrameworkCoreContext<TContext>` for framework-owned contexts that should not become the application unit of work; it registers only the DbContext. In ASP.NET Core applications, `Athenex.Whimclean.Api.AspNetCore` supplies a claims-based `ICurrentUser<TUserId>` via `AddClaimsCurrentUser<TUserId>`.

## Audit stamps and soft deletion

Both are opt-in per entity type, configured in `ConfigureModel` with the extensions in `Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence`:

| Call | Shadow properties added | Behavior |
| --- | --- | --- |
| `builder.HasAuditStamps()` | `CreatedAt` (`DateTimeOffset`), `CreatedBy` (`TActor`), `UpdatedAt` (`DateTimeOffset?`), `UpdatedBy` (nullable `TActor`) | Stamped on insert and update from `TimeProvider` and `IAuditActorResolver`. |
| `builder.HasSoftDelete()` | `IsDeleted` (`bool`), `DeletedAt` (`DateTimeOffset?`), `DeletedBy` (nullable `TActor`) | `Remove` becomes a soft delete; the named `SoftDeleteFilter` hides deleted rows. |

The property names are the constants in `AuditStampNames` and `SoftDeleteNames`; the annotations behind the calls are in `PersistenceAnnotations`. `CreatedBy` is required when `TActor` is a non-nullable value type such as `Guid` and nullable otherwise, including for every reference type such as `string`, because nullable reference annotations are not visible at runtime; the update and delete actors are always nullable. Owned types are skipped, and both calls must be made on the root entity type of an inheritance hierarchy, because EF Core allows query filters on root types only. When the entity already declares a CLR property with the same name and type, the context stamps that property instead of adding a shadow one, so an aggregate may expose `CreatedAt` when a business rule needs it.

Read a shadow value in a query by name, and configure its column in `ConfigureModel` if the defaults do not fit:

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

var newest = await dbContext.Orders
    .OrderByDescending(order => EF.Property<DateTimeOffset>(order, AuditStampNames.CreatedAt))
    .FirstAsync();

builder.Property<DateTimeOffset>(AuditStampNames.CreatedAt).HasColumnName("created_at");
```

### Soft delete

```csharp
dbContext.Orders.Remove(order);          // kept as a row: IsDeleted = true, DeletedAt/DeletedBy stamped
await dbContext.CommitAsync();

var deleted = await dbContext.Orders.IncludeSoftDeleted().SingleAsync(o => o.Id == id);
dbContext.Restore(deleted);              // IsDeleted = false; the save clears DeletedAt/DeletedBy
await dbContext.CommitAsync();

dbContext.HardDelete(order);             // deletes the row for real; reserve for authorized purges
await dbContext.CommitAsync();

var hidden = dbContext.IsSoftDeleted(order);
```

A soft delete on a type that also has audit stamps updates `UpdatedAt`/`UpdatedBy` as well. Because the row stays in the table, database cascades never fire: dependents that are tracked in the same context are soft-deleted (or deleted, for types without `HasSoftDelete()`) along with their principal, while dependents that were not loaded stay untouched and visible. Load the children you expect to disappear before calling `Remove`. `Restore` throws `InvalidOperationException` for entity types without `HasSoftDelete()`. To import rows that are already deleted, add them and set `dbContext.Entry(row).Property(SoftDeleteNames.IsDeleted).CurrentValue = true` before saving; the stamps are then written by that save.

## Persistence behavior

- Every added, modified, or deleted record requires a current user; otherwise the save throws `CurrentUserRequiredException`. Background work should supply an explicit system user, for example `StaticCurrentUser<TUserId>` from `Athenex.Whimclean.Application.Context`.
- Audit stamps and soft-delete marks are written to the properties described above from `TimeProvider` and `IAuditActorResolver`.
- `Remove` of an entity type configured with `HasSoftDelete()` is converted into a soft delete at save time; only the soft-delete flag and the stamps count as modified. `HardDelete` bypasses the conversion.
- `DbUpdateConcurrencyException` becomes `PersistenceConcurrencyException`.
- Aggregate domain events are snapshotted before the save, cleared from the aggregates once the save succeeds, and then dispatched with `CancellationToken.None`. A failing handler throws `DomainEventDispatchException` with the undelivered events in `UndeliveredEvents`; do not retry the save.
- Synchronous `SaveChanges` is intentionally unsupported. Use `SaveChangesAsync` or `IUnitOfWork.CommitAsync`.

For an outbox or atomic message-delivery guarantee, implement that application-specific pattern in the consuming context; direct post-save event dispatch is in-process only.

## Administrative queries

EF Core 10 named filters allow a specifically authorized query to disable only soft deletion:

```csharp
var includingDeleted = dbContext.Orders
    .IgnoreQueryFilters(["SoftDeleteFilter"]);
```

`IncludeSoftDeleted()` from `Athenex.Whimclean.Infrastructure.EntityFrameworkCore` does the same by name. Prefer either over bare `IgnoreQueryFilters()`, which would also drop tenant isolation.

This base package contains no PostgreSQL, Identity, FastEndpoints, or multitenancy dependency.

## List queries

The helpers in `Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries` turn a `ListQueryOptions` into a query that reads as filter, search, count, order, page, project:

```csharp
using Athenex.Whimclean.Application.Pagination;
using Athenex.Whimclean.Application.Queries;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries;
using Microsoft.EntityFrameworkCore;

private static readonly SortFields<Tenant> Sort = new()
{
    { "slug", tenant => tenant.Slug },
    { "name", tenant => tenant.Name },
    { "status", tenant => tenant.Status }
};

public async ValueTask<OffsetPagedResult<TenantResponse>> ListAsync(ListQueryOptions options, CancellationToken ct)
{
    var query = dbContext.Tenants.AsNoTracking();

    foreach (var filter in options.Filters)
    {
        query = (filter.Field, filter.Operator) switch
        {
            ("status", FilterOperator.Equal) when EnumFilter.TryParse<TenantStatus>(filter.Value, out var status) =>
                query.Where(tenant => tenant.Status == status),
            ("status", FilterOperator.In) => ApplyStatusIn(query, EnumFilter.Parse<TenantStatus>(filter.Values)),
            _ => query
        };
    }

    query = query.SearchLike(options.SearchTerm, tenant => tenant.Slug, tenant => tenant.Name);

    var total = await query.LongCountAsync(ct);
    var items = await Sort
        .Order(query, options.Sort, tenant => tenant.Name, tenant => tenant.Id)
        .Page(options)
        .Select(tenant => new TenantResponse(tenant.Id, tenant.Slug, tenant.Name, tenant.Status))
        .ToListAsync(ct);

    return items.ToPage(options, total);
}
```

- `SortFields<T>` maps the sort names an endpoint accepts to key selectors and applies the terms in sequence, skipping unknown names; the `Order` overload with a default key and tie-breaker never returns null and keeps pages stable.
- `SearchLike` matches any of the given string members with `EF.Functions.Like`, escaping `%`, `_` and `\` in the term through `LikePattern`. The pattern is sent as a query parameter, so the compiled query is reused across search terms. Case sensitivity follows the column collation; the PostgreSQL package adds a case-insensitive `SearchILike`.
- `Search` takes your own predicate builder, which receives the escaped contains pattern, when a search needs more than LIKE. `SearchPredicate.AnyMatch` composes a multi-field predicate over any matching function.
- `Page` applies `Offset` and `Size` to an ordered query; `ToPage` wraps the materialized items with the page number, size and total.
- `EnumFilter` from the Application package parses filter values into enum members, skipping unknown names the validator has already rejected.

## Apply migrations from the application image

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Operations;

var app = builder.Build();

if (DatabaseMigration.IsRequested(args))
{
    await app.Services.MigrateDatabaseAsync<ApplicationDbContext>();
    return;
}
```

A deployment then runs the same container once with `--migrate` (`DatabaseMigration.CommandLineSwitch`, matched case-insensitively) before starting it, instead of shipping the EF tooling to the server. Pending migrations are logged by name, and `MigrateDatabaseAsync` returns how many were applied.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
