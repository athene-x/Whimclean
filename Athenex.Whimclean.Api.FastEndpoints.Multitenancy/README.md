# Athenex.Whimclean.Api.FastEndpoints.Multitenancy

Optional ASP.NET Core tenant resolution and membership authorization for Whimclean APIs.

## Install

```shell
dotnet add package Athenex.Whimclean.Api.FastEndpoints.Multitenancy
```

The package depends on `Api.FastEndpoints` for consistent tenant error responses and on the dependency-free multitenancy contracts.

## Implement membership validation

The consuming application owns tenants and memberships. Implement its authorization rules through `ITenantAccessValidator<TUserId, TTenantId>`:

```csharp
using Athenex.Whimclean.Multitenancy;
using Microsoft.EntityFrameworkCore;

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

Include tenant state, membership expiry, suspension, or entitlement checks here when required.

## Register the API integration

```csharp
using Athenex.Whimclean.Api.FastEndpoints.Multitenancy;

builder.Services.AddFastEndpointsApi();
builder.Services.AddAspNetCoreMultitenancy<Guid, Guid>();
builder.Services.AddHeaderTenantResolution<Guid>(options =>
    options.HeaderName = "X-Tenant-Id");
builder.Services.AddScoped<
    ITenantAccessValidator<Guid, Guid>,
    TenantMembershipAccessValidator>();
```

The first generic argument is the current-user ID type. The second is the tenant ID type.

`AddAspNetCoreMultitenancy` registers:

- scoped requested/current tenant context
- the `TenantMember` authorization policy
- the membership authorization handler
- tenant exception mappings for the standard API error shape

## Middleware order

Authentication must establish the user before tenant membership authorization runs:

```csharp
app.UseAuthentication();
app.UseTenantResolution<Guid>();
app.UseAuthorization();
app.UseFastEndpointsApi();
```

The resolver only records a requested tenant. `ICurrentTenant<Guid>` remains empty until the authenticated user passes the access validator.

## Protect tenant endpoints

```csharp
using FastEndpoints;

public sealed class ListOrdersEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("orders");
        Policies(MultitenancyPolicies.TenantMember);
    }

    public override Task HandleAsync(CancellationToken cancellationToken) =>
        HttpContext.Response.SendOkAsync(cancellationToken);
}
```

Tenant-aware EF contexts resolved during the endpoint receive the authorized `ICurrentTenant<TTenantId>` from the same request scope.

## Use a route, host, or claim resolver

Replace the header resolver by implementing `IRequestedTenantResolver<TTenantId>`:

```csharp
public sealed class RouteTenantResolver
    : IRequestedTenantResolver<Guid>
{
    public bool TryResolve(HttpContext context, out Guid tenantId) =>
        Guid.TryParse(
            context.Request.RouteValues["tenantId"]?.ToString(),
            out tenantId);
}

services.AddScoped<IRequestedTenantResolver<Guid>, RouteTenantResolver>();
```

Do not call `AddHeaderTenantResolution` when a custom resolver is the only desired source.

## Error responses

- `CurrentTenantRequiredException` → 403 `Tenancy.CurrentTenantRequired`
- `TenantAccessViolationException` → 403 `Tenancy.AccessDenied`
- a missing or rejected requested tenant fails the `TenantMember` policy and produces the standard empty-403 mapping

The package does not create tenant tables, manage memberships, or decide whether Identity users are global or tenant-local. The recommended default is global authentication users plus consumer-owned tenant memberships.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
