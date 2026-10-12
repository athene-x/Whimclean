# Athenex.Whimclean

[ภาษาไทย](README-TH.md)

.NET 10 building blocks for domain-driven design, application-layer concerns, provider-neutral EF Core, PostgreSQL, optional multitenancy, and FastEndpoints APIs. Authentication is bring-your-own: applications authenticate with an external identity provider (Keycloak, Microsoft Entra ID, Auth0, or ASP.NET Core Identity wired directly) and Whimclean consumes the resulting principal through `ICurrentUser<TUserId>`; the optional `Identity.Keycloak` package ships the Keycloak registration. The repository produces these packages:

- [`Athenex.Whimclean.Domain`](src/Athenex.Whimclean.Domain/README.md) provides entities, aggregate roots, domain events, auditing, soft deletion, operation results, and text normalization.
- [`Athenex.Whimclean.Application`](src/Athenex.Whimclean.Application/README.md) provides current-context contracts, email abstractions, pagination models, persistence contracts, and validated query options with the `ListQueryParser` syntax. It depends on `Athenex.Whimclean.Domain`.
- [`Athenex.Whimclean.Infrastructure`](src/Athenex.Whimclean.Infrastructure/README.md) provides provider-neutral audit actor resolution, the in-process domain-event pipeline, and deterministic seed identifiers. It depends on `Athenex.Whimclean.Domain`.
- [`Athenex.Whimclean.Infrastructure.EntityFrameworkCore`](src/Athenex.Whimclean.Infrastructure.EntityFrameworkCore/README.md) provides required current-user writes, auditing, named soft-delete filters, concurrency handling, asynchronous domain-event dispatch, list query helpers, a `--migrate` switch, and common `DbContext`/`IUnitOfWork` registration. It depends on `Application`, `Infrastructure`, and EF Core (including `Microsoft.EntityFrameworkCore.Relational`), but no database provider.
- [`Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL`](src/Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL/README.md) provides the Npgsql dependency, PostgreSQL registration, and the case-insensitive `SearchILike` search helper. It depends on `Infrastructure.EntityFrameworkCore`.
- [`Athenex.Whimclean.Api.AspNetCore`](src/Athenex.Whimclean.Api.AspNetCore/README.md) provides the framework-neutral API conventions: a consistent error contract, exception handling, status-code responses, result-to-HTTP mapping, claims-based and local-table current-user resolution, health endpoints, per-caller rate limiting, and validated options. It depends on `Athenex.Whimclean.Application`.
- [`Athenex.Whimclean.Api.FastEndpoints`](src/Athenex.Whimclean.Api.FastEndpoints/README.md) provides FastEndpoints and OpenAPI registration with validation responses over the shared error contract, plus the `ListQueryRequest` contract and its validator. It depends on `Api.AspNetCore`.
- [`Athenex.Whimclean.Mediator`](src/Athenex.Whimclean.Mediator/README.md) optionally integrates [Mediator](https://github.com/martinothamar/Mediator): a pipeline behavior committing the unit of work after each command, and domain-event delivery as mediator notifications. It depends on `Application`, `Infrastructure`, and `Mediator.Abstractions`; the application supplies `Mediator.SourceGenerator`.
- [`Athenex.Whimclean.Multitenancy.Abstractions`](src/Athenex.Whimclean.Multitenancy.Abstractions/README.md) optionally provides tenant marker/current-context contracts, access validation, an ambient current tenant for non-HTTP hosts, and tenant boundary exceptions. It has no package dependencies.
- [`Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy`](src/Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy/README.md) optionally adds fail-closed tenant filters and write validation to the provider-neutral EF Core context.
- [`Athenex.Whimclean.Api.AspNetCore.Multitenancy`](src/Athenex.Whimclean.Api.AspNetCore.Multitenancy/README.md) optionally resolves an untrusted requested tenant from an id header or, through a consumer-owned slug lookup, a slug header, and establishes the current tenant only after a consumer-owned membership validator authorizes it. It works with FastEndpoints or Minimal APIs alike.
- [`Athenex.Whimclean.Auditing`](src/Athenex.Whimclean.Auditing/README.md) optionally records every audited command's outcome, actor and field changes in an audit log written outside the unit of work; it depends on `Application`, `Domain`, and `Mediator.Abstractions`. [`Athenex.Whimclean.Auditing.AspNetCore`](src/Athenex.Whimclean.Auditing.AspNetCore/README.md) feeds it the caller's name, correlation id, address and user agent from the request.
- [`Athenex.Whimclean.Infrastructure.Email.Smtp`](src/Athenex.Whimclean.Infrastructure.Email.Smtp/README.md) implements `IEmailSender` over SMTP with validated options. It depends on `Application`.
- [`Athenex.Whimclean.Identity.Keycloak`](src/Athenex.Whimclean.Identity.Keycloak/README.md) registers JWT bearer authentication with Keycloak's claim conventions and provides an admin API client for provisioning users. It depends on `Domain`.

## Installation

### Package feed

The packages are published to the athene-x [GitHub Packages](https://github.com/orgs/athene-x/packages) NuGet feed, not to nuget.org. Consuming them requires membership in the athene-x organization:

1. Create a [personal access token (classic)](https://github.com/settings/tokens) with the `read:packages` scope.
2. Register the feed once per machine:

   ```shell
   dotnet nuget add source \
     --username YOUR_GITHUB_USERNAME \
     --password YOUR_GITHUB_TOKEN \
     --store-password-in-clear-text \
     --name github \
     "https://nuget.pkg.github.com/athene-x/index.json"
   ```

3. If the consuming repository uses `packageSourceMapping` in its `nuget.config`, adding the source is not enough — restore refuses to consider unmapped sources. Also map the package prefix:

   ```xml
   <packageSourceMapping>
     <packageSource key="github">
       <package pattern="Athenex.Whimclean.*" />
     </packageSource>
   </packageSourceMapping>
   ```

### Packages

```shell
dotnet add package Athenex.Whimclean.Domain
dotnet add package Athenex.Whimclean.Application
dotnet add package Athenex.Whimclean.Infrastructure
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL
dotnet add package Athenex.Whimclean.Api.AspNetCore
dotnet add package Athenex.Whimclean.Api.FastEndpoints
dotnet add package Athenex.Whimclean.Mediator
dotnet add package Athenex.Whimclean.Multitenancy.Abstractions
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy
dotnet add package Athenex.Whimclean.Api.AspNetCore.Multitenancy
dotnet add package Athenex.Whimclean.Auditing
dotnet add package Athenex.Whimclean.Auditing.AspNetCore
dotnet add package Athenex.Whimclean.Infrastructure.Email.Smtp
dotnet add package Athenex.Whimclean.Identity.Keycloak
```

Install only the top-level packages an application uses. For example, a PostgreSQL API can reference `Infrastructure.EntityFrameworkCore.PostgreSQL` and `Api.FastEndpoints`; NuGet supplies their lower-level dependencies transitively.

### Common package combinations

| Application need | Direct package references |
| --- | --- |
| Domain and application contracts only | `Domain`, `Application` |
| PostgreSQL business API | `Infrastructure.EntityFrameworkCore.PostgreSQL`, `Api.FastEndpoints` |
| Minimal APIs instead of FastEndpoints | `Infrastructure.EntityFrameworkCore.PostgreSQL`, `Api.AspNetCore` |
| Add tenant-aware EF isolation | previous packages plus `Infrastructure.EntityFrameworkCore.Multitenancy` |
| Add HTTP tenant resolution and membership authorization | previous packages plus `Api.AspNetCore.Multitenancy` |
| Commit each command through Mediator | previous packages plus `Mediator` (and `Mediator.SourceGenerator` from the application) |
| Audit every command | previous packages plus `Auditing` and `Auditing.AspNetCore` |
| Authenticate with Keycloak and provision its users | previous packages plus `Identity.Keycloak` |
| Send email over SMTP | previous packages plus `Infrastructure.Email.Smtp` |

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

Infrastructure.Email.Smtp
    -> Application

Api.FastEndpoints
    -> Api.AspNetCore
        -> Application

Api.AspNetCore.Multitenancy
    -> Api.AspNetCore
    -> Multitenancy.Abstractions

Mediator
    -> Application
    -> Infrastructure

Auditing.AspNetCore
    -> Auditing
        -> Application
        -> Domain

Identity.Keycloak
    -> Domain
```

The PostgreSQL project contains no auditing, tenant, soft-delete, concurrency, or domain-event pipeline. Common persistence behaviors live in the provider-neutral EF Core package, while tenant behavior is an optional decorator package. A future provider package therefore needs only its provider dependency and a registration method that delegates to `AddEntityFrameworkCoreUnitOfWork<TContext>`.

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

Raised events are exposed through `DomainEvents`. Clearing them is an infrastructure concern: `ClearDomainEvents()` is reachable only through `IHasDomainEvents`, and the EF Core context calls it after a successful save, before dispatching the events.

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

`AuditableEntity<TId, TActor>` and `AuditableAggregateRoot<TId, TActor>` declare these four fields once, so entities derived from them do not redeclare them. The EF Core context stamps the values at save time.

The optional `Auditing` package records command outcomes rather than row fields. A Mediator command implements `IAuditedCommand`, the `AuditCommandBehavior` writes an `AuditLog<TUserId, TTenantId>` entry through the application's `IAuditLogWriter<TUserId, TTenantId>` outside the command's unit of work, and domain-event handlers add field changes through `IAuditChangeCollector`:

```csharp
using Athenex.Whimclean.Auditing;
using Athenex.Whimclean.Auditing.AspNetCore;
using Athenex.Whimclean.Domain.Results;
using Athenex.Whimclean.Mediator;
using Mediator;

public sealed record SuspendTenantCommand(Guid TenantId, string Reason)
    : ICommand<Result>, IAuditedCommand
{
    public AuditDescriptor Describe() => new(
        Action: "Tenant.Suspend",
        EntityType: "Tenant",
        EntityId: TenantId.ToString(),
        TenantId: TenantId,
        Reason: Reason);
}

builder.Services.AddAuditing<Guid, Guid>();
builder.Services.AddUnitOfWorkBehavior();
builder.Services.AddScoped<IAuditLogWriter<Guid, Guid>, AuditLogWriter>();
builder.Services.AddHttpAuditRequestContext();
```

`AddAuditing` must run before `AddUnitOfWorkBehavior` so the audit behavior wraps the unit of work and sees the committed outcome. `AddHttpAuditRequestContext` from `Auditing.AspNetCore` replaces the default `NullAuditRequestContext` with the request's actor name, correlation id (`X-Correlation-Id`, `X-Request-Id`, or the trace identifier), remote address and user agent.

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

`ListQueryParser` builds the same options from raw query-string values. Filters use `field:operator:value` with the operators `eq`, `ne`, `gt`, `ge`, `lt`, `le`, `cn`, `sw`, `ew` and `in` (comma-separated values); sort is a comma-separated field list with a `-` prefix for descending. `EnumFilter`, in the same namespace, parses filter values into enum members for translation:

```csharp
var parsed = ListQueryParser.Parse(
    searchTerm: "pending",
    sort: "-createdAt",
    filters: ["status:in:Pending,Paid"],
    page: 1,
    size: 20);
```

In a FastEndpoints API, derive the request from `ListQueryRequest` and its validator from `ListQueryRequestValidator<TRequest>` to allow-list the sort and filter fields of each endpoint; `ToListQueryOptions()` then parses the validated request:

```csharp
using Athenex.Whimclean.Api.FastEndpoints.Queries;
using Athenex.Whimclean.Application.Queries;

public sealed class ListOrdersRequest : ListQueryRequest;

public sealed class ListOrdersRequestValidator
    : ListQueryRequestValidator<ListOrdersRequest>
{
    public ListOrdersRequestValidator()
    {
        AllowSortFields("createdAt", "total");
        AllowEnumFilterField<OrderStatus>("status");
        AllowFilterField(
            "createdAt",
            FilterOperator.GreaterThanOrEqual,
            FilterOperator.LessThan);
    }
}
```

The EF Core package translates the options: `SortFields<T>` maps allowed sort names to key selectors, `SearchLike` (or `SearchILike` from the PostgreSQL package) searches several columns with escaped wildcards, and `Page`/`ToPage` apply the offset and wrap the result:

```csharp
using Athenex.Whimclean.Application.Queries;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL.Queries;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries;
using Microsoft.EntityFrameworkCore;

private static readonly SortFields<Order> OrderSort = new()
{
    { "createdAt", order => order.CreatedAt },
    { "total", order => order.Total }
};

var query = dbContext.Orders
    .SearchILike(options.SearchTerm, order => order.Number, order => order.CustomerName);

var totalItems = await query.LongCountAsync(cancellationToken);

var items = await OrderSort
    .Order(
        query,
        options.Sort,
        order => order.CreatedAt,
        order => order.Id,
        SortDirection.Descending)
    .Page(options)
    .ToListAsync(cancellationToken);

return items.ToPage(options, totalItems);
```

## Pagination

```csharp
using Athenex.Whimclean.Application.Pagination;

var result = new OffsetPagedResult<string>(
    items: ["first", "second"],
    page: 1,
    size: 20,
    totalItems: 2);
```

`Map` projects the items of a page while keeping its page number, size and totals; `KeysetPagedResult<T, TKey>` is the cursor-based counterpart.

## Email

Implement `IEmailSender` in the infrastructure layer, or register the shipped SMTP implementation, and consume it from application use cases:

```csharp
var message = new EmailMessage(
    recipient: "member@example.com",
    subject: "Welcome",
    textBody: "Your account is ready.");

await emailSender.SendAsync(message, cancellationToken);
```

`Infrastructure.Email.Smtp` sends messages through `System.Net.Mail` with options for host, port, credentials, TLS and the default sender. Bind `SmtpEmailOptions` from the `Email` section and validate it at startup:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Configuration;
using Athenex.Whimclean.Infrastructure.Email.Smtp;

builder.Services.AddValidatedOptions<SmtpEmailOptions>(
    builder.Configuration,
    SmtpEmailOptions.SectionName,
    options => options.Validate());
builder.Services.AddSmtpEmailSender();
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
services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(connectionString);
```

`AddNullableAuditActorResolver<Guid>()`, `AddIdentityAuditActorResolver<Guid>()`, `AddDomainEventDispatcher()` and `AddMediatorDomainEventDispatcher()` are equivalent shortcuts with `TryAdd` semantics.

PostgreSQL-specific Npgsql options stay at the provider edge:

```csharp
services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(
    connectionString,
    postgreSql => postgreSql.MigrationsAssembly("MyApp.Migrations.PostgreSQL"));
```

To use another provider before a dedicated Whimclean adapter exists, reference `Athenex.Whimclean.Infrastructure.EntityFrameworkCore` plus that provider and configure it through the common registration method:

```csharp
services.AddEntityFrameworkCoreUnitOfWork<ApplicationDbContext>((_, options) =>
    options.UseSqlServer(connectionString));
```

This retains the same `AuditableDbContext`, soft-delete filters, concurrency boundary, domain-event dispatch, and `IUnitOfWork` registration. Add the optional EF multitenancy package when tenant isolation is required. Only the provider configuration changes.

The application image can apply its own migrations. `DatabaseMigration.IsRequested(args)` detects the `--migrate` switch and `MigrateDatabaseAsync<TContext>` applies the pending migrations, logging their names and returning how many ran:

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Operations;

var app = builder.Build();

if (DatabaseMigration.IsRequested(args))
{
    await app.Services.MigrateDatabaseAsync<ApplicationDbContext>();
    return;
}
```

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

`Identity.Keycloak` replaces the hand-written JWT bearer setup with `AddKeycloakAuthentication`, bound from the `Keycloak` section (`Authority`, `Audience`, optional `MetadataAddress`, `RequireHttpsMetadata`, `NameClaimType`, `RoleClaimType`). It turns inbound claim mapping off so `sub` stays `sub`, pins the valid issuer to `Authority`, and registers authorization services. `AddKeycloakAdministration` adds `IKeycloakUserAdministration`, a typed client over the admin API that uses the service account in the `KeycloakAdmin` section:

```csharp
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Identity.Keycloak;

builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAdministration(builder.Configuration);
builder.Services.AddClaimsCurrentUser<Guid>(options => options.ClaimType = "sub");
```

When the application keeps its own user table and the token only carries the identity provider's subject, use `AddLocalUserResolution<TUserId>` instead of the claims current user. The middleware maps the subject claim to the local id through the application's `ILocalUserLookup<TUserId>` and records it on a scoped `RequestCurrentUser<TUserId>`; a subject without a local user leaves the current user unset. Place the middleware after authentication and before anything that reads the current user:

```csharp
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Api.AspNetCore.Context;

builder.Services.AddLocalUserResolution<Guid>(options => options.ClaimType = "sub");
builder.Services.AddScoped<ILocalUserLookup<Guid>, LocalUserLookup>();

app.UseAuthentication();
app.UseLocalUserResolution<Guid>();
app.UseAuthorization();
```

Every added, modified, or deleted record requires `ICurrentUser<TUserId>` to provide a user, including records that do not implement an audit interface. Background jobs should register an explicit system-user implementation such as `StaticCurrentUser<TUserId>` instead of silently bypassing the write boundary; `ICurrentUserInitializer<TUserId>` lets bootstrap code set the user of the scoped `RequestCurrentUser<TUserId>`. `IdentityAuditActorResolver<TActor>` is available when the current-user ID and stored actor use the same non-null type.

`AuditableDbContext<TUserId, TActor>` supports asynchronous saves only so domain events are never forced through sync-over-async execution.

Application mappings in `ConfigureModel` run before infrastructure query filters. This allows the base context to discover entities registered only by `ApplyConfigurationsFromAssembly` and then apply the `SoftDeleteFilter` to every root entity implementing `ISoftDelete<TActor>`. The filter hides deleted rows; it does not convert a physical `Remove` into a soft delete. Instead, removing a soft-deletable entity is rejected with `InvalidOperationException`, so the entity's own soft-delete operation must be used. `IncludeSoftDeleted()` disables only this filter for one query.

EF Core optimistic-concurrency failures are exposed as `PersistenceConcurrencyException`, keeping EF-specific exceptions inside infrastructure while allowing the API layer to return an HTTP 409 response.

### Optional multitenancy

Install the optional EF and API adapters only in tenant-aware applications. The consumer owns its `Tenant` and `TenantMembership` entities, their lifecycle, and the membership query; Whimclean supplies the boundaries that connect those decisions to authorization and EF isolation. This section covers the shipped logical-isolation model; the [multitenancy setup guide](docs/multitenancy-setup.md) walks through all three isolation models — logical (shared schema), schema per tenant, and database per tenant.

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

The header resolver treats its value only as a requested tenant, and it requires the header exactly once: a missing or repeated header resolves nothing. `ICurrentTenant<TTenantId>` remains unset until the authenticated user passes `ITenantAccessValidator`; callers therefore cannot select an unauthorized tenant merely by changing a header. A host, route, or claim resolver can replace `IRequestedTenantResolver<TTenantId>` without changing the EF layer.

Deployments that serve each tenant from a path or hostname forward the slug in a header. `AddSlugTenantResolution()` (non-generic) configures the slug header, which defaults to `X-Tenant-Slug`, and `UseSlugTenantResolution<TTenantId>()` maps the trimmed, lower-cased slug to the tenant id through the application's `ITenantSlugLookup<TTenantId>`. The slug middleware records nothing when a requested tenant already exists, so an id header takes precedence when both are configured:

```csharp
builder.Services.AddSlugTenantResolution();
builder.Services.AddScoped<ITenantSlugLookup<Guid>, TenantSlugLookup>();

app.UseAuthentication();
app.UseTenantResolution<Guid>();
app.UseSlugTenantResolution<Guid>();
app.UseAuthorization();
```

Hosts without HTTP tenant resolution (workers, seeders) register `AmbientCurrentTenant<TTenantId>` and wrap tenant-scoped work in `AmbientCurrentTenant<TTenantId>.Begin(tenantId)`.

Entities implementing `IMultiTenant<TTenantId>` receive a fail-closed named `TenantFilter`. Without an authorized current tenant, queries return no tenant records. Added, modified, or deleted tenant records must match the current tenant and may not change `TenantId`; the context validates but never rewrites it. Administrative queries must remain separately authorized and can disable only the required filter on EF Core 10:

```csharp
var includingDeleted = dbContext.Orders
    .IgnoreQueryFilters(["SoftDeleteFilter"]);

var acrossTenants = dbContext.Orders
    .IgnoreQueryFilters(["TenantFilter"]);
```

The other named filter remains active in each query. `QueryFilterNames.SoftDelete` and `QueryFilterNames.Tenant` hold the two filter names.

## FastEndpoints API

Register FastEndpoints, OpenAPI, the Whimclean exception handler, and ASP.NET Core problem-details services:

```csharp
builder.Services.AddFastEndpointsApi();
```

Apply the default `/api/v1` route prefix, exception middleware, empty-status handling, whole-value binding of list query filters, and Whimclean error response contract:

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

Infrastructure boundary exceptions, validation failures, and empty authorization/routing responses use the same `ApiErrorResponse` shape:

| Condition | HTTP | Error code |
| --- | ---: | --- |
| `CurrentUserRequiredException` | 401 | `Authentication.CurrentUserRequired` |
| `PersistenceConcurrencyException` | 409 | `Persistence.ConcurrentUpdate` |
| FastEndpoints validation failure | 400 | `Validation.Failed` |
| Empty 401 response | 401 | `Authentication.Unauthorized` |
| Empty 403 response | 403 | `Authorization.Forbidden` |
| Empty 404 response | 404 | `Resource.NotFound` |
| Unexpected exception | 500 | `Server.UnexpectedError` |

Installing and registering `Api.AspNetCore.Multitenancy` adds `CurrentTenantRequiredException` → 403 `Tenancy.CurrentTenantRequired` and `TenantAccessViolationException` → 403 `Tenancy.AccessDenied` without coupling the base API packages to multitenancy.

Unexpected exception details are logged but are not returned to clients.

`Api.AspNetCore` also ships the operations helpers an API host needs: `MapHealthEndpoints` maps the anonymous `/health/live` and `/health/ready` endpoints (readiness runs the checks tagged `HealthCheckTags.Ready`; both are exempt from rate limiting), `AddRequestRateLimiting` adds a global fixed window per caller from the `RateLimiting` section (`PermitLimit`, default 300; `WindowSeconds`, default 60; validated through `RequestRateLimitOptions.Validate()`) paired with `app.UseRateLimiter()`, `AddValidatedOptions` binds an options class and validates it at startup, and `ConfigurationPlaceholder.IsUnset` detects the `<set-via-user-secrets>` marker left in committed settings (`AddDbContextCheck` comes from `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`):

```csharp
using Athenex.Whimclean.Api.AspNetCore.Operations;

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(tags: [HealthCheckTags.Ready]);
builder.Services.AddRequestRateLimiting(builder.Configuration);

app.UseAuthentication();
app.UseRateLimiter();
app.MapHealthEndpoints();
```
