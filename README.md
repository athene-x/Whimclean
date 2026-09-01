# Athenex.Whimclean

.NET 10 building blocks for domain-driven design, application-layer concerns, provider-neutral EF Core, PostgreSQL, optional multitenancy, and FastEndpoints APIs. Authentication is bring-your-own: applications authenticate with an external identity provider (Keycloak, Microsoft Entra ID, Auth0, or ASP.NET Core Identity wired directly) and Whimclean consumes the resulting principal through `ICurrentUser<TUserId>`. The repository produces these packages:

- [`Athenex.Whimclean.Domain`](Athenex.Whimclean.Domain/README.md) provides entities, aggregate roots, domain events, auditing, soft deletion, and operation results.
- [`Athenex.Whimclean.Application`](Athenex.Whimclean.Application/README.md) provides current-context contracts, email abstractions, pagination models, persistence contracts, and validated query options. It depends on `Athenex.Whimclean.Domain`.
- [`Athenex.Whimclean.Infrastructure`](Athenex.Whimclean.Infrastructure/README.md) provides provider-neutral audit actor resolution and domain-event dispatch contracts. It depends on `Athenex.Whimclean.Domain`.
- [`Athenex.Whimclean.Infrastructure.EntityFrameworkCore`](Athenex.Whimclean.Infrastructure.EntityFrameworkCore/README.md) provides required current-user writes, auditing, named soft-delete filters, concurrency handling, asynchronous domain-event dispatch, and common `DbContext`/`IUnitOfWork` registration. It depends on `Application`, `Infrastructure`, and EF Core, but no database provider.
- [`Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL`](Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL/README.md) provides only the Npgsql dependency and PostgreSQL registration. It depends on `Infrastructure.EntityFrameworkCore`.
- [`Athenex.Whimclean.Api.FastEndpoints`](Athenex.Whimclean.Api.FastEndpoints/README.md) provides FastEndpoints and OpenAPI registration, a consistent API error contract, exception handling, empty-status handling, and result-to-HTTP mapping. It depends on `Athenex.Whimclean.Application`.
- [`Athenex.Whimclean.Multitenancy.Abstractions`](Athenex.Whimclean.Multitenancy.Abstractions/README.md) optionally provides tenant marker/current-context contracts, access validation, and tenant boundary exceptions. It has no package dependencies.
- [`Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy`](Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy/README.md) optionally adds fail-closed tenant filters and write validation to the provider-neutral EF Core context.
- [`Athenex.Whimclean.Api.FastEndpoints.Multitenancy`](Athenex.Whimclean.Api.FastEndpoints.Multitenancy/README.md) optionally resolves an untrusted requested tenant and establishes the current tenant only after a consumer-owned membership validator authorizes it.

## Installation

```shell
dotnet add package Athenex.Whimclean.Domain --version 0.1.0
dotnet add package Athenex.Whimclean.Application --version 0.1.0
dotnet add package Athenex.Whimclean.Infrastructure --version 0.1.0
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore --version 0.1.0
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL --version 0.1.0
dotnet add package Athenex.Whimclean.Api.FastEndpoints --version 0.1.0
dotnet add package Athenex.Whimclean.Multitenancy.Abstractions --version 0.1.0
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy --version 0.1.0
dotnet add package Athenex.Whimclean.Api.FastEndpoints.Multitenancy --version 0.1.0
```

Install only the top-level packages an application uses. For example, a PostgreSQL API can reference `Infrastructure.EntityFrameworkCore.PostgreSQL` and `Api.FastEndpoints`; NuGet supplies their lower-level dependencies transitively.

### Common package combinations

| Application need | Direct package references |
| --- | --- |
| Domain and application contracts only | `Domain`, `Application` |
| PostgreSQL business API | `Infrastructure.EntityFrameworkCore.PostgreSQL`, `Api.FastEndpoints` |
| Add tenant-aware EF isolation | previous packages plus `Infrastructure.EntityFrameworkCore.Multitenancy` |
| Add HTTP tenant resolution and membership authorization | previous packages plus `Api.FastEndpoints.Multitenancy` |

Each package directory contains a focused README with installation, responsibility boundaries, and usage examples. The remainder of this root guide shows how the packages compose in one application.

## Infrastructure package boundaries

The dependency direction is intentionally one-way:

```text
Infrastructure.EntityFrameworkCore.PostgreSQL
    -> Infrastructure.EntityFrameworkCore
        -> Infrastructure
        -> Application
            -> Domain

Infrastructure.EntityFrameworkCore.Multitenancy
    -> Infrastructure.EntityFrameworkCore
    -> Multitenancy.Abstractions

Api.FastEndpoints.Multitenancy
    -> Api.FastEndpoints
    -> Multitenancy.Abstractions
```

The PostgreSQL project contains no auditing, tenant, soft-delete, concurrency, or domain-event pipeline. Common persistence behaviors live in the provider-neutral EF Core package, while tenant behavior is an optional decorator package. A future provider package therefore needs only its provider dependency and a registration method that delegates to `AddEntityFrameworkCoreDbContext<TContext>`.

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

## EF Core and database providers

Derive a non-tenant application context from `AuditableDbContext<TUserId, TActor>`. `TUserId` is the authenticated user identifier, while `TActor` is the representation stored in audit fields.

```csharp
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
    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
```

Register the current-user implementation and domain-event dispatcher. `NullableAuditActorResolver<TUserId>` converts a non-null value-type user ID such as `Guid` into nullable audit fields such as `Guid?`:

```csharp
services.AddSingleton(TimeProvider.System);
services.AddClaimsCurrentUser<Guid>();
services.AddSingleton<
    IAuditActorResolver<Guid, Guid?>,
    NullableAuditActorResolver<Guid>>();
services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();
services.AddPostgreSqlDbContext<ApplicationDbContext>(connectionString);
```

PostgreSQL-specific Npgsql options stay at the provider edge:

```csharp
services.AddPostgreSqlDbContext<ApplicationDbContext>(
    connectionString,
    postgreSql => postgreSql.MigrationsAssembly("MyApp.Migrations.PostgreSQL"));
```

To use another provider before a dedicated Whimclean adapter exists, reference `Athenex.Whimclean.Infrastructure.EntityFrameworkCore` plus that provider and configure it through the common registration method:

```csharp
services.AddEntityFrameworkCoreDbContext<ApplicationDbContext>((_, options) =>
    options.UseSqlServer(connectionString));
```

This retains the same `AuditableDbContext`, soft-delete filters, concurrency boundary, domain-event dispatch, and `IUnitOfWork` registration. Add the optional EF multitenancy package when tenant isolation is required. Only the provider configuration changes.

### Authentication and the current user

Whimclean does not store users, credentials, or tokens. Authenticate with an external identity provider (Keycloak, Microsoft Entra ID, Auth0) or wire ASP.NET Core Identity directly against Microsoft's own APIs; either way, the library consumes only the authenticated principal through `ICurrentUser<TUserId>`.

For any scheme that populates `HttpContext.User`, `Api.AspNetCore` supplies the claims-based implementation:

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://keycloak.example.com/realms/whimclean";
        options.Audience = "whimclean-api";
    });

builder.Services.AddClaimsCurrentUser<Guid>();
```

The default claim type is `ClaimTypes.NameIdentifier`; pass `options => options.ClaimType = "sub"` when inbound claim mapping is disabled. Registration uses `TryAddScoped`, so an application-supplied `ICurrentUser<TUserId>` wins.

Every added, modified, or deleted record requires `ICurrentUser<TUserId>` to provide a user, including records that do not implement an audit interface. Background jobs should register an explicit system-user implementation instead of silently bypassing the write boundary. `IdentityAuditActorResolver<TActor>` is available when the current-user ID and stored actor use the same non-null type.

`AuditableDbContext<TUserId, TActor>` supports asynchronous saves only so domain events are never forced through sync-over-async execution.

Application mappings in `ConfigureModel` run before infrastructure query filters. This allows the base context to discover entities registered only by `ApplyConfigurationsFromAssembly` and then apply the `SoftDeleteFilter` to every root entity implementing `ISoftDelete<TActor>`. The filter hides deleted rows; it does not convert a physical `Remove` into a soft delete.

EF Core optimistic-concurrency failures are exposed as `PersistenceConcurrencyException`, keeping EF-specific exceptions inside infrastructure while allowing the API layer to return an HTTP 409 response.

### Optional multitenancy

Install the optional EF and API adapters only in tenant-aware applications. The consumer owns its `Tenant` and `TenantMembership` entities, their lifecycle, and the membership query; Whimclean supplies the boundaries that connect those decisions to authorization and EF isolation.

Tenant business contexts derive from `MultiTenantDbContext<TUserId, TActor, TTenantId>`:

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy.Persistence;
using Athenex.Whimclean.Multitenancy;

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
    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
```

Implement the access validator in the consuming infrastructure project. This is where an application applies its own membership status, tenant status, expiry, role, or suspension rules:

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

Register the optional adapters in the API composition root:

```csharp
builder.Services.AddFastEndpointsApi();
builder.Services.AddAspNetCoreMultitenancy<Guid, Guid>();
builder.Services.AddHeaderTenantResolution<Guid>(options =>
    options.HeaderName = "X-Tenant-Id");
builder.Services.AddScoped<
    ITenantAccessValidator<Guid, Guid>,
    TenantMembershipAccessValidator>();
```

Resolve the requested tenant after authentication and before authorization, then protect tenant endpoints with the supplied policy:

```csharp
app.UseAuthentication();
app.UseTenantResolution<Guid>();
app.UseAuthorization();
app.UseFastEndpointsApi();

public sealed class ListOrdersEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("orders");
        Policies(MultitenancyPolicies.TenantMember);
    }
}
```

The header resolver treats its value only as a requested tenant. `ICurrentTenant<TTenantId>` remains unset until the authenticated user passes `ITenantAccessValidator`; callers therefore cannot select an unauthorized tenant merely by changing a header. A host, route, or claim resolver can replace `IRequestedTenantResolver<TTenantId>` without changing the EF layer.

Entities implementing `IMultiTenant<TTenantId>` receive a fail-closed named `TenantFilter`. Without an authorized current tenant, queries return no tenant records. Added, modified, or deleted tenant records must match the current tenant; the context validates but never rewrites `TenantId`. Administrative queries must remain separately authorized and can disable only the required filter on EF Core 10:

```csharp
var includingDeleted = dbContext.Orders
    .IgnoreQueryFilters(["SoftDeleteFilter"]);

var acrossTenants = dbContext.Orders
    .IgnoreQueryFilters(["TenantFilter"]);
```

The other named filter remains active in each query.

## FastEndpoints API

Register FastEndpoints, OpenAPI, the Whimclean exception handler, and ASP.NET Core problem-details services:

```csharp
builder.Services.AddFastEndpointsApi();
```

Apply the default `/api/v1` route prefix, exception middleware, empty-status handling, and Whimclean error response contract:

```csharp
app.UseFastEndpointsApi();
```

Applications can change the prefix and add product-specific FastEndpoints configuration:

```csharp
app.UseFastEndpointsApi(
    routePrefix: "api/v2",
    configure: options =>
    {
        options.Security.PermissionsClaimType = "permission";
    });
```

Map application errors inside an endpoint with `result.Error.ToHttpResult()`.

Infrastructure boundary exceptions and empty authorization/routing responses use the same `ApiErrorResponse` shape:

| Condition | HTTP | Error code |
| --- | ---: | --- |
| `CurrentUserRequiredException` | 401 | `Authentication.CurrentUserRequired` |
| `PersistenceConcurrencyException` | 409 | `Persistence.ConcurrentUpdate` |
| Empty 401 response | 401 | `Authentication.Unauthorized` |
| Empty 403 response | 403 | `Authorization.Forbidden` |
| Empty 404 response | 404 | `Resource.NotFound` |
| Unexpected exception | 500 | `Server.UnexpectedError` |

Installing and registering `Api.FastEndpoints.Multitenancy` adds `CurrentTenantRequiredException` → 403 `Tenancy.CurrentTenantRequired` and `TenantAccessViolationException` → 403 `Tenancy.AccessDenied` without coupling the base FastEndpoints package to multitenancy.

Unexpected exception details are logged but are not returned to clients.
