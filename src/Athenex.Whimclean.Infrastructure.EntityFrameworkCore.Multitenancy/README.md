# Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy

Optional, provider-neutral EF Core tenant isolation built on `AuditableDbContext`.

## Install

```shell
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy
```

Install a database provider separately, such as `Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL`.

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

Tenant-owned entities implement `IMultiTenant<TTenantId>`:

```csharp
public sealed class Order : IMultiTenant<Guid>
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
}
```

## Register the context

PostgreSQL:

```csharp
services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(connectionString);
```

Another EF provider:

```csharp
services.AddEntityFrameworkCoreUnitOfWork<ApplicationDbContext>((_, options) =>
    options.UseSqlServer(connectionString));
```

Register an `ICurrentTenant<Guid>` implementation. The ASP.NET Core multitenancy adapter supplies one after membership authorization.

## Enforcement behavior

- `IMultiTenant<TTenantId>` entities receive the named `TenantFilter`.
- Queries return no tenant-owned records when there is no authorized current tenant.
- Added, modified, and deleted tenant-owned records require a current tenant.
- A record whose `TenantId` differs from the current tenant — or whose `TenantId` was modified, in either direction — causes `TenantAccessViolationException`. Tenant reassignment is never a valid write.
- The context validates but never assigns or rewrites `TenantId`.
- Auditing, current-user enforcement, soft deletion, concurrency translation, and domain-event behavior remain inherited from `AuditableDbContext`.

An explicitly authorized administrative query can disable only the tenant filter:

```csharp
var acrossTenants = dbContext.Orders
    .IgnoreQueryFilters(["TenantFilter"]);
```

Disabling a filter is not authorization. Keep cross-tenant operations behind a separate administrative use case and policy.

This package does not resolve tenants, manage memberships, or depend on PostgreSQL or Identity.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
