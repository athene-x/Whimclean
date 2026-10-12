# Athenex.Whimclean.Api.AspNetCore.Multitenancy

[ภาษาไทย](README-TH.md)

Optional ASP.NET Core tenant resolution and membership authorization for Whimclean APIs.

## Install

```shell
dotnet add package Athenex.Whimclean.Api.AspNetCore.Multitenancy
```

The package depends on `Athenex.Whimclean.Api.AspNetCore` for consistent tenant error responses and on the dependency-free multitenancy contracts in `Athenex.Whimclean.Multitenancy.Abstractions`. It contains no FastEndpoints code and works with FastEndpoints or Minimal APIs alike.

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

Include tenant state, membership expiry, suspension, or entitlement checks here when required. Implementations should fail closed: return `false` for unknown users or tenants.

## Register the API integration

```csharp
using Athenex.Whimclean.Api.AspNetCore.Multitenancy;
using Athenex.Whimclean.Api.FastEndpoints;
using Athenex.Whimclean.Multitenancy;

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

- the scoped `TenantContext<TTenantId>`, exposed as both `IRequestedTenant<TTenantId>` and `ICurrentTenant<TTenantId>`
- the `TenantMember` authorization policy (the name can be changed through the `policyName` parameter; `MultitenancyPolicies.TenantMember` holds the default)
- the membership authorization handler
- tenant exception mappings for the standard API error shape

`AddHeaderTenantResolution<TTenantId>` registers `HeaderRequestedTenantResolver<TTenantId>` as the `IRequestedTenantResolver<TTenantId>` (via `TryAddScoped`, so an existing resolver wins). The header name defaults to `X-Tenant-Id` (`HeaderTenantResolutionOptions.HeaderName`), and the value is parsed with the invariant culture. Exactly one header value is required: a missing header resolves nothing, and a repeated header is treated as ambiguous and resolves nothing.

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
using Athenex.Whimclean.Api.AspNetCore.Multitenancy;
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
using Athenex.Whimclean.Api.AspNetCore.Multitenancy;
using Microsoft.AspNetCore.Http;

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
- a missing or rejected requested tenant fails the `TenantMember` policy and produces the standard 403 body (`Authorization.Forbidden`)

The package does not create tenant tables, manage memberships, or decide whether Identity users are global or tenant-local. The recommended default is global authentication users plus consumer-owned tenant memberships.

## Resolve the tenant from a slug

Deployments that serve each tenant from a path (`/acme/...`) or hostname forward the slug in a header; the API maps it to the tenant id:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Multitenancy;
using Microsoft.EntityFrameworkCore;

public sealed class TenantSlugLookup(ApplicationDbContext dbContext) : ITenantSlugLookup<Guid>
{
    public async ValueTask<Guid> FindBySlugAsync(string slug, CancellationToken ct = default) =>
        await dbContext.Tenants
            .Where(tenant => tenant.Slug == slug)
            .Select(tenant => tenant.Id)
            .SingleOrDefaultAsync(ct);
}

builder.Services.AddAspNetCoreMultitenancy<Guid, Guid>();
builder.Services.AddHeaderTenantResolution<Guid>();          // optional: X-Tenant-Id still works
builder.Services.AddSlugTenantResolution();                  // X-Tenant-Slug
builder.Services.AddScoped<ITenantSlugLookup<Guid>, TenantSlugLookup>();

app.UseAuthentication();
app.UseTenantResolution<Guid>();
app.UseSlugTenantResolution<Guid>();
app.UseAuthorization();
```

`AddSlugTenantResolution` is not generic: it only registers `SlugTenantResolutionOptions` (header name, default `X-Tenant-Slug`); the tenant id type is given to `UseSlugTenantResolution<TTenantId>` and `ITenantSlugLookup<TTenantId>`. The slug arrives trimmed and lower-cased. The slug middleware records nothing when a tenant was already resolved, so an explicit id header wins, and a default (empty) identifier means no such tenant. As with the id header, exactly one slug header value is required; a repeated header resolves nothing. Like the header resolver it only records a claim; membership is still validated by the `TenantMember` policy.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
