# Athenex.Whimclean

[ภาษาไทย](README-TH.md)

Whimclean is a set of .NET 10 building blocks for business applications: a domain model with aggregates, domain events and audit fields; application-layer contracts for queries, pagination and email; provider-neutral EF Core persistence with a PostgreSQL adapter; optional multitenancy; and an ASP.NET Core Minimal API host with a FastEndpoints adapter.

Three decisions shape the library:

- **Authentication is bring-your-own.** Applications authenticate with an external identity provider (Keycloak, Microsoft Entra ID, Auth0) or with ASP.NET Core Identity, and Whimclean consumes only the resulting principal through `ICurrentUser<TUserId>`. It stores no users, credentials or tokens.
- **Dependencies point one way.** Provider, API and tenant packages depend on the neutral packages, never the reverse, so a provider or endpoint style can be swapped without touching the domain or application layers.
- **One API contract.** `Athenex.Whimclean.Api.AspNetCore` is the primary API package, built on Minimal APIs. `Athenex.Whimclean.Api.FastEndpoints` adapts the same error body, validation responses and list query contract to FastEndpoints.

## Contents

- [Packages](#packages)
- [Installation](#installation)
- [Composing an application](#composing-an-application)
- [Domain model](#domain-model)
- [Application contracts](#application-contracts)
- [Persistence with EF Core](#persistence-with-ef-core)
- [Authentication and the current user](#authentication-and-the-current-user)
- [Commands, events and the audit log](#commands-events-and-the-audit-log)
- [Multitenancy](#multitenancy)
- [API host](#api-host)
- [Further reading](#further-reading)

## Packages

Each package has its own README with installation, boundaries and examples.

### Foundation

| Package | Provides | Depends on |
| --- | --- | --- |
| [`Domain`](src/Athenex.Whimclean.Domain/README.md) | Entities, aggregate roots, domain events, audit fields, soft deletion, `Result` and `Error`, text normalization. | none |
| [`Application`](src/Athenex.Whimclean.Application/README.md) | Current-user and current-context contracts, email abstractions, pagination models, persistence contracts, validated list query options and the `ListQueryParser` syntax. | `Domain` |
| [`Infrastructure`](src/Athenex.Whimclean.Infrastructure/README.md) | Provider-neutral audit actor resolution, the in-process domain-event pipeline, deterministic seed identifiers. | `Domain` |
| [`Multitenancy.Abstractions`](src/Athenex.Whimclean.Multitenancy.Abstractions/README.md) | Tenant marker and current-tenant contracts, access validation, an ambient current tenant for non-HTTP hosts, tenant boundary exceptions. | none |

### Persistence

| Package | Provides | Depends on |
| --- | --- | --- |
| [`Infrastructure.EntityFrameworkCore`](src/Athenex.Whimclean.Infrastructure.EntityFrameworkCore/README.md) | `AuditableDbContext`, required current-user writes, audit stamping, named soft-delete filters, concurrency translation, asynchronous domain-event dispatch, list query helpers, the `--migrate` switch, `DbContext`/`IUnitOfWork` registration. | `Application`, `Infrastructure`, EF Core (no provider) |
| [`Infrastructure.EntityFrameworkCore.PostgreSQL`](src/Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL/README.md) | Npgsql, PostgreSQL registration, the case-insensitive `SearchILike` helper. | `Infrastructure.EntityFrameworkCore` |
| [`Infrastructure.EntityFrameworkCore.Multitenancy`](src/Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy/README.md) | `MultiTenantDbContext`, fail-closed tenant filters and write validation. | `Infrastructure.EntityFrameworkCore`, `Multitenancy.Abstractions` |

### API

| Package | Provides | Depends on |
| --- | --- | --- |
| [`Api.AspNetCore`](src/Athenex.Whimclean.Api.AspNetCore/README.md) | The primary API package: Minimal API registration with OpenAPI, FluentValidation request validation on the `api/v1` group, the `ListQueryRequest` contract, the error contract with exception and `Result` mapping, current-user resolution, health endpoints, rate limiting, validated options. | `Application` |
| [`Api.FastEndpoints`](src/Athenex.Whimclean.Api.FastEndpoints/README.md) | The FastEndpoints adapter: FastEndpoints and its OpenAPI registration, validation responses over the shared error contract, whole-value binding of list query filters. | `Api.AspNetCore` |
| [`Api.AspNetCore.Multitenancy`](src/Athenex.Whimclean.Api.AspNetCore.Multitenancy/README.md) | Requested-tenant resolution from an id or slug header, membership authorization, the authorized tenant context, tenant error mappings. Works with Minimal APIs and FastEndpoints alike. | `Api.AspNetCore`, `Multitenancy.Abstractions` |

### Cross-cutting

| Package | Provides | Depends on |
| --- | --- | --- |
| [`Mediator`](src/Athenex.Whimclean.Mediator/README.md) | Integration with [Mediator](https://github.com/martinothamar/Mediator): a pipeline behavior committing the unit of work after each command, and domain-event delivery as notifications. The application supplies `Mediator.SourceGenerator`. | `Application`, `Infrastructure`, `Mediator.Abstractions` |
| [`Auditing`](src/Athenex.Whimclean.Auditing/README.md) | An audit log of every audited command's outcome, actor and field changes, written outside the unit of work. | `Application`, `Domain`, `Mediator.Abstractions` |
| [`Auditing.AspNetCore`](src/Athenex.Whimclean.Auditing.AspNetCore/README.md) | Caller name, correlation id, address and user agent for the audit log, taken from the request. | `Auditing` |
| [`Infrastructure.Email.Smtp`](src/Athenex.Whimclean.Infrastructure.Email.Smtp/README.md) | `IEmailSender` over SMTP with validated options. | `Application` |
| [`Identity.Keycloak`](src/Athenex.Whimclean.Identity.Keycloak/README.md) | JWT bearer authentication with Keycloak's claim conventions and an admin API client for provisioning users. | `Domain` |

### Dependency graph

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

The PostgreSQL package contains no auditing, tenant, soft-delete, concurrency or domain-event code; those behaviors live in the provider-neutral EF Core package, and tenant behavior is a separate decorator package. A new provider package therefore needs only its provider dependency and a registration method that delegates to `AddEntityFrameworkCoreUnitOfWork<TContext>`.

### Choosing packages

Reference only the top-level packages an application uses; NuGet supplies their dependencies transitively.

| Application need | Direct package references |
| --- | --- |
| Domain and application contracts only | `Domain`, `Application` |
| PostgreSQL business API (Minimal APIs) | `Infrastructure.EntityFrameworkCore.PostgreSQL`, `Api.AspNetCore` |
| FastEndpoints instead of Minimal APIs | `Infrastructure.EntityFrameworkCore.PostgreSQL`, `Api.FastEndpoints` |
| Add tenant-aware EF isolation | previous packages plus `Infrastructure.EntityFrameworkCore.Multitenancy` |
| Add HTTP tenant resolution and membership authorization | previous packages plus `Api.AspNetCore.Multitenancy` |
| Commit each command through Mediator | previous packages plus `Mediator` (and `Mediator.SourceGenerator` from the application) |
| Audit every command | previous packages plus `Auditing` and `Auditing.AspNetCore` |
| Authenticate with Keycloak and provision its users | previous packages plus `Identity.Keycloak` |
| Send email over SMTP | previous packages plus `Infrastructure.Email.Smtp` |

## Installation

### Package feed

The packages are published to the athene-x [GitHub Packages](https://github.com/orgs/athene-x/packages) NuGet feed, not to nuget.org. Consuming them requires membership in the athene-x organization.

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

3. If the consuming repository uses `packageSourceMapping` in its `nuget.config`, restore considers only mapped sources. Map the package prefix as well:

   ```xml
   <packageSourceMapping>
     <packageSource key="github">
       <package pattern="Athenex.Whimclean.*" />
     </packageSource>
   </packageSourceMapping>
   ```

### Adding packages

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

## Composing an application

A PostgreSQL business API authenticated by Keycloak composes the packages like this. Each part is explained in the sections that follow.

```csharp
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Api.AspNetCore.Operations;
using Athenex.Whimclean.Identity.Keycloak;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Operations;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL;
using Athenex.Whimclean.Mediator;

var builder = WebApplication.CreateBuilder(args);

// Authentication and the current user
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddClaimsCurrentUser<Guid>(options => options.ClaimType = "sub");

// Persistence
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddNullableAuditActorResolver<Guid>();
builder.Services.AddMediatorDomainEventDispatcher();
builder.Services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(
    builder.Configuration.GetConnectionString("Default")!);

// Commands
builder.Services.AddMediator();
builder.Services.AddUnitOfWorkBehavior();

// API host
builder.Services.AddMinimalApi();
builder.Services.AddApiValidatorsFromAssemblyContaining<Program>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(tags: [HealthCheckTags.Ready]);
builder.Services.AddRequestRateLimiting(builder.Configuration);

var app = builder.Build();

if (DatabaseMigration.IsRequested(args))
{
    await app.Services.MigrateDatabaseAsync<ApplicationDbContext>();
    return;
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseMinimalApi();
app.MapHealthEndpoints();

var api = app.MapApiGroup();
api.MapGet("orders/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetOrderQuery(id));
    return result.ToHttpResult(order => Results.Ok(order));
});

app.Run();
```

## Domain model

### Entities and aggregate roots

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

### Audit fields

`IAuditable<TActor>` declares the four audit fields. The actor type carries its own nullability, so an application can store an identifier, a name or a custom actor type:

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

`AuditableEntity<TId, TActor>` and `AuditableAggregateRoot<TId, TActor>` declare the fields once, so derived entities do not repeat them. The EF Core context stamps the values at save time.

### Results

```csharp
using Athenex.Whimclean.Domain.Results;

Result<Guid> CreateOrder(bool isValid) => isValid
    ? Result<Guid>.Success(Guid.NewGuid())
    : Result<Guid>.Failure(
        Error.Validation("Order.Invalid", "The order is invalid."));
```

Reading `Value` on a failed result throws `InvalidOperationException`; check `IsSuccess` or `IsFailure` first. The API package maps each `ErrorType` to an HTTP status code (see [Error contract](#error-contract)).

## Application contracts

### List queries

`ListQueryOptions` is the validated, normalized input of an offset-paginated list query. It checks its own limits and exposes materialized read-only collections, so infrastructure code translates only approved fields and operators into database queries:

```csharp
using Athenex.Whimclean.Application.Queries;

var options = new ListQueryOptions(
    searchTerm: "pending",
    sort: [new SortTerm("createdAt", SortDirection.Descending)],
    filters: [new FilterTerm("status", "Pending")],
    page: 1,
    size: 20);
```

`ListQueryParser` builds the same options from raw query-string values:

```csharp
var parsed = ListQueryParser.Parse(
    searchTerm: "pending",
    sort: "-createdAt",
    filters: ["status:in:Pending,Paid"],
    page: 1,
    size: 20);
```

| Input | Syntax |
| --- | --- |
| `sort` | Comma-separated field names; `-` prefix for descending. |
| `filters` | One `field:operator:value` per entry. Operators: `eq`, `ne`, `gt`, `ge`, `lt`, `le`, `cn` (contains), `sw`, `ew`, `in` (comma-separated values). |

`EnumFilter`, in the same namespace, parses filter values into enum members for translation.

**In the API**, derive the request from `ListQueryRequest` and its validator from `ListQueryRequestValidator<TRequest>` (both in `Athenex.Whimclean.Api.AspNetCore.Queries`) to allow-list the sort and filter fields of each endpoint. `ToListQueryOptions()` parses the validated request:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Queries;
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

api.MapGet("orders", async (
    [AsParameters] ListOrdersRequest request,
    IOrderQueries queries,
    CancellationToken cancellationToken) =>
    Results.Ok(await queries.ListAsync(request.ToListQueryOptions(), cancellationToken)));
```

A Minimal API binds the request with `[AsParameters]`, and the `MapApiGroup` validation filter runs the validator registered by `AddApiValidatorsFromAssembly`. A FastEndpoints endpoint takes the same request as its DTO, and FastEndpoints discovers the validator.

**In persistence**, the EF Core package translates the options: `SortFields<T>` maps allowed sort names to key selectors, `SearchLike` (or `SearchILike` from the PostgreSQL package) searches several columns with escaped wildcards, and `Page`/`ToPage` apply the offset and wrap the result:

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

### Pagination

```csharp
using Athenex.Whimclean.Application.Pagination;

var result = new OffsetPagedResult<string>(
    items: ["first", "second"],
    page: 1,
    size: 20,
    totalItems: 2);
```

`Map` projects the items of a page while keeping its page number, size and totals. `KeysetPagedResult<T, TKey>` is the cursor-based counterpart.

### Email

Application use cases send through `IEmailSender`. Implement it in the infrastructure layer, or register the shipped SMTP implementation:

```csharp
var message = new EmailMessage(
    recipient: "member@example.com",
    subject: "Welcome",
    textBody: "Your account is ready.");

await emailSender.SendAsync(message, cancellationToken);
```

`Infrastructure.Email.Smtp` sends through `System.Net.Mail` with options for host, port, credentials, TLS and the default sender. Bind `SmtpEmailOptions` from the `Email` section and validate it at startup:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Configuration;
using Athenex.Whimclean.Infrastructure.Email.Smtp;

builder.Services.AddValidatedOptions<SmtpEmailOptions>(
    builder.Configuration,
    SmtpEmailOptions.SectionName,
    options => options.Validate());
builder.Services.AddSmtpEmailSender();
```

## Persistence with EF Core

### The application context

Derive a non-tenant application context from `AuditableDbContext<TUserId, TActor>`. `TUserId` is the authenticated user identifier; `TActor` is the representation stored in audit fields.

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

The context behaves as follows:

- **Every write needs a user.** Added, modified or deleted records require `ICurrentUser<TUserId>` to provide a user, including records without an audit interface. Background jobs register an explicit system user such as `StaticCurrentUser<TUserId>` rather than bypassing the boundary.
- **Audit fields are stamped at save time**, through the `IAuditActorResolver<TUserId, TActor>`.
- **Soft-deleted rows are hidden.** Application mappings in `ConfigureModel` run before the infrastructure filters, so the base context discovers every root entity implementing `ISoftDelete<TActor>` and applies the named `SoftDeleteFilter`. A physical `Remove` of such an entity is rejected with `InvalidOperationException`; use the entity's own soft-delete operation. `IncludeSoftDeleted()` disables the filter for one query.
- **Domain events are dispatched after a successful save**, then cleared. Saves are asynchronous only, so events are never forced through sync-over-async execution.
- **Concurrency failures are translated.** `DbUpdateConcurrencyException` surfaces as `PersistenceConcurrencyException`, which the API layer maps to HTTP 409.

### Registration

```csharp
services.AddSingleton(TimeProvider.System);
services.AddClaimsCurrentUser<Guid>();
services.AddSingleton<
    IAuditActorResolver<Guid, Guid?>,
    NullableAuditActorResolver<Guid>>();
services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();
services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(connectionString);
```

`NullableAuditActorResolver<TUserId>` converts a non-null value-type user id such as `Guid` into nullable audit fields such as `Guid?`; `IdentityAuditActorResolver<TActor>` applies when the user id and the stored actor share one non-null type. The shortcuts `AddNullableAuditActorResolver<Guid>()`, `AddIdentityAuditActorResolver<Guid>()`, `AddDomainEventDispatcher()` and `AddMediatorDomainEventDispatcher()` register the same services with `TryAdd` semantics.

Npgsql options stay at the provider edge:

```csharp
services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(
    connectionString,
    postgreSql => postgreSql.MigrationsAssembly("MyApp.Migrations.PostgreSQL"));
```

### Other providers

Reference `Athenex.Whimclean.Infrastructure.EntityFrameworkCore` plus the provider and configure it through the common registration method:

```csharp
services.AddEntityFrameworkCoreUnitOfWork<ApplicationDbContext>((_, options) =>
    options.UseSqlServer(connectionString));
```

The `AuditableDbContext`, soft-delete filters, concurrency boundary, domain-event dispatch and `IUnitOfWork` registration are the same; only the provider configuration differs. Add the EF multitenancy package when tenant isolation is required.

### Migrations

The application image applies its own migrations. `DatabaseMigration.IsRequested(args)` detects the `--migrate` switch and `MigrateDatabaseAsync<TContext>` applies the pending migrations, logging their names and returning how many ran:

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Operations;

var app = builder.Build();

if (DatabaseMigration.IsRequested(args))
{
    await app.Services.MigrateDatabaseAsync<ApplicationDbContext>();
    return;
}
```

## Authentication and the current user

Whimclean stores no users, credentials or tokens. Authenticate with an external identity provider (Keycloak, Microsoft Entra ID, Auth0) or wire ASP.NET Core Identity directly against Microsoft's APIs; either way, the library consumes only the authenticated principal through `ICurrentUser<TUserId>`.

### Current user from claims

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

### Keycloak

`Identity.Keycloak` replaces the hand-written JWT bearer setup with `AddKeycloakAuthentication`, bound from the `Keycloak` section (`Authority`, `Audience`, optional `MetadataAddress`, `RequireHttpsMetadata`, `NameClaimType`, `RoleClaimType`). It turns inbound claim mapping off so `sub` stays `sub`, pins the valid issuer to `Authority`, and registers authorization services. `AddKeycloakAdministration` adds `IKeycloakUserAdministration`, a typed client over the admin API that uses the service account in the `KeycloakAdmin` section:

```csharp
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Identity.Keycloak;

builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAdministration(builder.Configuration);
builder.Services.AddClaimsCurrentUser<Guid>(options => options.ClaimType = "sub");
```

### Current user from a local user table

When the application keeps its own user table and the token carries only the identity provider's subject, use `AddLocalUserResolution<TUserId>` instead of the claims current user. The middleware maps the subject claim to the local id through the application's `ILocalUserLookup<TUserId>` and records it on a scoped `RequestCurrentUser<TUserId>`; a subject without a local user leaves the current user unset. Place the middleware after authentication and before anything that reads the current user:

```csharp
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Api.AspNetCore.Context;

builder.Services.AddLocalUserResolution<Guid>(options => options.ClaimType = "sub");
builder.Services.AddScoped<ILocalUserLookup<Guid>, LocalUserLookup>();

app.UseAuthentication();
app.UseLocalUserResolution<Guid>();
app.UseAuthorization();
```

`ICurrentUserInitializer<TUserId>` lets bootstrap code set the user of the scoped `RequestCurrentUser<TUserId>` after resolving its own user.

## Commands, events and the audit log

### Mediator

`Athenex.Whimclean.Mediator` integrates [Mediator](https://github.com/martinothamar/Mediator). `AddUnitOfWorkBehavior` registers a pipeline behavior that commits the unit of work after each successful command, and `MediatorDomainEventDispatcher` (registered by `AddMediatorDomainEventDispatcher`) delivers domain events as Mediator notifications. The application supplies `Mediator.SourceGenerator`.

### Command audit log

The optional `Auditing` package records command outcomes rather than row fields. A command implements `IAuditedCommand`; `AuditCommandBehavior` writes an `AuditLog<TUserId, TTenantId>` entry through the application's `IAuditLogWriter<TUserId, TTenantId>` outside the command's unit of work; domain-event handlers add field changes through `IAuditChangeCollector`:

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

`AddAuditing` must run before `AddUnitOfWorkBehavior` so the audit behavior wraps the unit of work and sees the committed outcome. `AddHttpAuditRequestContext` from `Auditing.AspNetCore` replaces the default `NullAuditRequestContext` with the request's actor name, correlation id (`X-Correlation-Id`, `X-Request-Id` or the trace identifier), remote address and user agent.

## Multitenancy

Install the EF and API multitenancy packages only in tenant-aware applications. The consumer owns its `Tenant` and `TenantMembership` entities, their lifecycle and the membership query; Whimclean supplies the boundaries that connect those decisions to authorization and EF isolation. This section covers the shipped logical-isolation model; the [multitenancy setup guide](docs/multitenancy-setup.md) walks through all three models: logical (shared schema), schema per tenant and database per tenant.

### Tenant context

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

Entities implementing `IMultiTenant<TTenantId>` receive the fail-closed named `TenantFilter`: without an authorized current tenant, queries return no tenant records. Added, modified or deleted tenant records must match the current tenant and may not change `TenantId`; the context validates but never rewrites it.

Administrative queries stay separately authorized and disable only the filter they need:

```csharp
var includingDeleted = dbContext.Orders
    .IgnoreQueryFilters(["SoftDeleteFilter"]);

var acrossTenants = dbContext.Orders
    .IgnoreQueryFilters(["TenantFilter"]);
```

The other named filter remains active in each query. `QueryFilterNames.SoftDelete` and `QueryFilterNames.Tenant` hold the two filter names.

### Access validation

Implement the access validator in the consuming infrastructure project. This is where an application applies its own membership status, tenant status, expiry, role or suspension rules:

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

### Tenant resolution in the API

Register the API integration in the composition root:

```csharp
builder.Services.AddMinimalApi();
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
app.UseMinimalApi();

app.MapApiGroup()
    .MapGet("orders", (IOrderQueries queries) => Results.Ok(queries.List()))
    .RequireAuthorization(MultitenancyPolicies.TenantMember);
```

A FastEndpoints endpoint applies the same policy with `Policies(MultitenancyPolicies.TenantMember)` in its `Configure` method.

The header resolver treats its value only as a *requested* tenant and requires the header exactly once: a missing or repeated header resolves nothing. `ICurrentTenant<TTenantId>` remains unset until the authenticated user passes `ITenantAccessValidator`, so callers cannot select an unauthorized tenant by changing a header. A host, route or claim resolver can replace `IRequestedTenantResolver<TTenantId>` without changing the EF layer.

### Slug resolution

Deployments that serve each tenant from a path or hostname forward the slug in a header. `AddSlugTenantResolution()` configures the slug header (default `X-Tenant-Slug`), and `UseSlugTenantResolution<TTenantId>()` maps the trimmed, lower-cased slug to the tenant id through the application's `ITenantSlugLookup<TTenantId>`. The slug middleware records nothing when a requested tenant already exists, so an id header takes precedence when both are configured:

```csharp
builder.Services.AddSlugTenantResolution();
builder.Services.AddScoped<ITenantSlugLookup<Guid>, TenantSlugLookup>();

app.UseAuthentication();
app.UseTenantResolution<Guid>();
app.UseSlugTenantResolution<Guid>();
app.UseAuthorization();
```

### Non-HTTP hosts

Workers and seeders register `AmbientCurrentTenant<TTenantId>` and wrap tenant-scoped work in `AmbientCurrentTenant<TTenantId>.Begin(tenantId)`.

## API host

### Minimal API

`Api.AspNetCore` registers the shared error handling, an OpenAPI document and the application's FluentValidation validators, then maps endpoints into the `/api/v1` group, which validates every handler argument that has a registered validator:

```csharp
builder.Services.AddMinimalApi();
builder.Services.AddApiValidatorsFromAssemblyContaining<Program>();

app.UseMinimalApi();

var api = app.MapApiGroup();
api.MapGet("orders/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetOrderQuery(id));
    return result.ToHttpResult(order => Results.Ok(order));
});
```

| Need | Call |
| --- | --- |
| Another route prefix | `MapApiGroup("api/v2")` |
| No OpenAPI endpoint | `UseMinimalApi(openApiRoutePattern: null)` |
| Customize the document | `AddMinimalApi(openApi => ...)` |
| Validate endpoints mapped outside the group | `.WithRequestValidation()` |

### FastEndpoints adapter

Applications written with FastEndpoints register the adapter instead. It builds on the primary package, so the error contract, list query contract and validation responses are identical:

```csharp
builder.Services.AddFastEndpointsApi();

app.UseFastEndpointsApi();
```

`UseFastEndpointsApi` applies the default `/api/v1` route prefix, the shared error handling, whole-value binding of list query filters and the Whimclean validation response. The prefix and FastEndpoints configuration are adjustable:

```csharp
app.UseFastEndpointsApi(
    routePrefix: "api/v2",
    configure: options =>
    {
        options.Security.PermissionsClaimType = "permission";
    });
```

### Error contract

Application errors become HTTP responses with `result.ToHttpResult(...)` or `error.ToHttpResult()`: `Validation` 400, `Unauthorized` 401, `Forbidden` 403, `NotFound` 404, `Conflict` 409, `Failure` 500.

Infrastructure boundary exceptions, validation failures and empty authorization or routing responses use the same `ApiErrorResponse` body:

```json
{
  "code": "Resource.NotFound",
  "description": "The requested resource was not found.",
  "errors": null
}
```

| Condition | HTTP | Error code |
| --- | ---: | --- |
| `CurrentUserRequiredException` | 401 | `Authentication.CurrentUserRequired` |
| `PersistenceConcurrencyException` | 409 | `Persistence.ConcurrentUpdate` |
| Validation failure (Minimal API filter or FastEndpoints) | 400 | `Validation.Failed` |
| Empty 400 response | 400 | `Request.Invalid` |
| Empty 401 response | 401 | `Authentication.Unauthorized` |
| Empty 403 response | 403 | `Authorization.Forbidden` |
| Empty 404 response | 404 | `Resource.NotFound` |
| Unexpected exception | 500 | `Server.UnexpectedError` |

`Api.AspNetCore.Multitenancy` adds `CurrentTenantRequiredException` → 403 `Tenancy.CurrentTenantRequired` and `TenantAccessViolationException` → 403 `Tenancy.AccessDenied` without coupling the base API packages to multitenancy. Unexpected exception details are logged, never returned to clients.

### Operations

`Api.AspNetCore` also ships the helpers an API host needs:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Operations;

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(tags: [HealthCheckTags.Ready]);
builder.Services.AddRequestRateLimiting(builder.Configuration);

app.UseAuthentication();
app.UseRateLimiter();
app.MapHealthEndpoints();
```

| Helper | Behavior |
| --- | --- |
| `MapHealthEndpoints` | Maps the anonymous `/health/live` and `/health/ready` endpoints; readiness runs the checks tagged `HealthCheckTags.Ready`; both are exempt from rate limiting. `AddDbContextCheck` comes from `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`. |
| `AddRequestRateLimiting` | A global fixed window per caller from the `RateLimiting` section (`PermitLimit`, default 300; `WindowSeconds`, default 60), validated through `RequestRateLimitOptions.Validate()`. Pair with `app.UseRateLimiter()`. |
| `AddValidatedOptions` | Binds an options class to a section and validates it at startup. |
| `ConfigurationPlaceholder.IsUnset` | Detects the `<set-via-user-secrets>` marker left in committed settings. |

## Further reading

- Package READMEs, linked in [Packages](#packages).
- [Multitenancy setup guide](docs/multitenancy-setup.md): logical, schema-per-tenant and database-per-tenant isolation.
- [Upgrading to 1.3.0](docs/upgrading-to-1.3.md): replacing application-written helpers with the packages.
