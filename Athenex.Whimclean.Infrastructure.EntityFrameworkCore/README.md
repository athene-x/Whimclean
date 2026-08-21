# Athenex.Whimclean.Infrastructure.EntityFrameworkCore

Provider-neutral EF Core persistence for auditing, soft-delete filtering, concurrency translation, domain-event dispatch, and unit-of-work registration.

## Install

```shell
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer # example provider
```

Use the PostgreSQL adapter instead of referencing Npgsql directly when PostgreSQL is the target database.

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
    }
}
```

`TUserId` is the trusted current-user identifier. `TActor` is the value stored in audit fields and may be nullable.

## Register with any EF Core provider

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore;

services.AddSingleton(TimeProvider.System);
services.AddScoped<ICurrentUser<Guid>, HttpCurrentUser>();
services.AddSingleton<
    IAuditActorResolver<Guid, Guid?>,
    NullableAuditActorResolver<Guid>>();
services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

services.AddEntityFrameworkCoreDbContext<ApplicationDbContext>((_, options) =>
    options.UseSqlServer(connectionString));
```

`AddEntityFrameworkCoreDbContext<TContext>` requires `IUnitOfWork` and registers the context as that interface. Use `AddEntityFrameworkCoreContext<TContext>` for framework-owned contexts such as Identity contexts that should not become the application unit of work.

## Persistence behavior

- Every added, modified, or deleted record requires a current user. Background work should supply an explicit system-user implementation.
- `ICreateAuditable`, `IUpdateAuditable`, and `ISoftDelete` fields are populated from `TimeProvider` and `IAuditActorResolver`.
- `ISoftDelete` entities receive the named `SoftDeleteFilter`; physical deletes are not automatically converted into soft deletes.
- `DbUpdateConcurrencyException` becomes `PersistenceConcurrencyException`.
- Aggregate domain events are dispatched after a successful database save and then cleared.
- Synchronous `SaveChanges` is intentionally unsupported. Use `SaveChangesAsync` or `IUnitOfWork.CommitAsync`.

For an outbox or atomic message-delivery guarantee, implement that application-specific pattern in the consuming context; direct post-save event dispatch is in-process only.

## Administrative queries

EF Core 10 named filters allow a specifically authorized query to disable only soft deletion:

```csharp
var includingDeleted = dbContext.Orders
    .IgnoreQueryFilters(["SoftDeleteFilter"]);
```

This base package contains no PostgreSQL, Identity, FastEndpoints, or multitenancy dependency.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
