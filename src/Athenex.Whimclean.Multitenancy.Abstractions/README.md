# Athenex.Whimclean.Multitenancy.Abstractions

[ภาษาไทย](README-TH.md)

Dependency-free contracts for optional multitenancy. The consuming application owns tenant management; this package defines only integration boundaries. All types live in the `Athenex.Whimclean.Multitenancy` namespace.

## Install

```shell
dotnet add package Athenex.Whimclean.Multitenancy.Abstractions
```

Most applications install an EF or ASP.NET Core multitenancy adapter instead and receive this package transitively.

## Mark tenant-owned entities

```csharp
using Athenex.Whimclean.Multitenancy;

public sealed class Order : IMultiTenant<Guid>
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
}
```

`IMultiTenant<TTenantId>` indicates data ownership through a read-only `TenantId`. The optional EF adapter uses it for query filtering and write validation.

## Requested tenant versus current tenant

- `IRequestedTenant<TTenantId>` represents an untrusted tenant selected by a header, route, host, or similar request input.
- `ICurrentTenant<TTenantId>` represents the tenant established only after authorization.

Both expose `TryGetTenantId(out TTenantId tenantId)`, which returns `false` when no tenant is present. Keeping these contracts separate prevents request input from automatically becoming an authorized data boundary.

`AmbientCurrentTenant<TTenantId>` is an `ICurrentTenant<TTenantId>` backed by an async-local scope for hosts without HTTP tenant resolution, such as background workers or seeders: wrap tenant-scoped work in `using (AmbientCurrentTenant<Guid>.Begin(tenantId))`. Scopes nest, disposing restores the previous tenant, and the caller vouches for the tenant because no access validation happens there.

## Consumer-owned access validation

Implement `ITenantAccessValidator<TUserId, TTenantId>` against the application’s own membership storage and rules:

```csharp
public sealed class TenantMembershipAccessValidator(
    AuthenticationDbContext dbContext)
    : ITenantAccessValidator<Guid, Guid>
{
    public Task<bool> HasAccessAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        dbContext.TenantMemberships.AnyAsync(
            membership =>
                membership.UserId == userId &&
                membership.TenantId == tenantId &&
                membership.IsActive,
            cancellationToken);
}
```

Implementations should fail closed and return `false` for unknown users or tenants. The consumer owns:

- `Tenant` and `TenantMembership` entities
- tenant creation, suspension, and deletion
- membership invitation, roles, state, and expiry
- subscription or entitlement rules
- administrative cross-tenant authorization

The package also defines `CurrentTenantRequiredException` (tenant-owned changes are about to be persisted without an established current tenant) and `TenantAccessViolationException` (a save would change a record owned by another tenant) for fail-closed persistence boundaries.

This package has no Domain, Application, EF Core, ASP.NET Core, Identity, or database-provider dependency.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
