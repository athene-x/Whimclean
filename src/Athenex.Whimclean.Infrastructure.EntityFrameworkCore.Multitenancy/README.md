# Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy

[ภาษาไทย](README-TH.md)

Optional, provider-neutral EF Core tenant isolation built on `AuditableDbContext`.

## Install

```shell
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy
```

Install a database provider separately, such as `Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL`. The package references the provider-neutral EF Core package and `Athenex.Whimclean.Multitenancy.Abstractions`.

## Create a tenant-aware context

```csharp
using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Infrastructure.Auditing;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy.Persistence;
using Athenex.Whimclean.Multitenancy;
using Microsoft.EntityFrameworkCore;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    TimeProvider timeProvider,
    ICurrentUser<Guid> currentUser,
    IAuditActorResolver<Guid, Guid?> auditActorResolver,
    IDomainEventDispatcher domainEventDispatcher,
    ICurrentTenant<Guid> currentTenant)
    : MultiTenantDbContext<Guid, Guid?, Guid>(
        options,
        timeProvider,
        currentUser,
        auditActorResolver,
        domainEventDispatcher,
        currentTenant)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
```

`MultiTenantDbContext<TUserId, TActor, TTenantId>` seals `ConfigureAdditionalInfrastructureModel` and `ValidateInfrastructureWrites` to install the tenant filter and write validation; `ConfigureModel` and `ValidateApplicationWrites` remain available to the application.

Tenant-owned entities implement `IMultiTenant<TTenantId>`:

```csharp
public sealed class Order : IMultiTenant<Guid>
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
}
```

## Register the context

This package adds no registration method of its own; use the one of your provider.

PostgreSQL:

```csharp
services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(connectionString);
```

Another EF provider:

```csharp
services.AddEntityFrameworkCoreUnitOfWork<ApplicationDbContext>((_, options) =>
    options.UseSqlServer(connectionString));
```

Register an `ICurrentTenant<Guid>` implementation. The ASP.NET Core multitenancy adapter supplies one through `AddAspNetCoreMultitenancy<TUserId, TTenantId>` after membership authorization; non-HTTP hosts can register `AmbientCurrentTenant<TTenantId>` from the abstractions package.

## Enforcement behavior

- Root `IMultiTenant<TTenantId>` entity types receive the named `TenantFilter` (`QueryFilterNames.Tenant`); owned types are skipped.
- Queries return no tenant-owned records when there is no authorized current tenant: the filter requires an established tenant and a matching `TenantId`, so it fails closed.
- Added, modified, and deleted tenant-owned records require a current tenant; otherwise the save throws `CurrentTenantRequiredException`. Saves that touch no tenant-owned entity are not checked.
- A record whose `TenantId` differs from the current tenant — or whose `TenantId` was modified, in either direction — causes `TenantAccessViolationException`. Tenant reassignment is never a valid write.
- The context validates but never assigns or rewrites `TenantId`.
- Audit stamps, current-user enforcement, soft deletion (`HasAuditStamps()` and `HasSoftDelete()` in `ConfigureModel`), concurrency translation, and domain-event behavior remain inherited from `AuditableDbContext`.

An explicitly authorized administrative query can disable only the tenant filter:

```csharp
var acrossTenants = dbContext.Orders
    .IgnoreQueryFilters(["TenantFilter"]);
```

Disabling a filter is not authorization. Keep cross-tenant operations behind a separate administrative use case and policy. `IncludeSoftDeleted()` keeps the tenant filter active; bare `IgnoreQueryFilters()` drops both filters.

This package does not resolve tenants, manage memberships, or depend on PostgreSQL or Identity.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
