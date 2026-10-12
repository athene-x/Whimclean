# Multitenancy setup guide

[ภาษาไทย](multitenancy-setup-TH.md)

How to set up a Whimclean application for each of the three tenant-isolation models:

| | Case 1: Logical isolation | Case 2: Schema isolation | Case 3: Database isolation |
| --- | --- | --- | --- |
| Data layout | One database, one schema, `TenantId` column on every tenant-owned row | One database, one PostgreSQL schema per tenant | One database per tenant |
| Enforced by | Whimclean query filter + write validation | PostgreSQL `search_path` set per connection | Connection string chosen per request |
| Library support | Fully shipped | Consumer-implemented today (interceptor pattern below; a packaged helper is planned) | Consumer-implemented today (connection-resolver pattern below; a packaged `ITenantStore` helper is planned) |
| Isolation strength | Application-level | Database-object level | Physical |
| Migration effort | One migration run | One run per schema | One run per database |
| Cost / density | Highest density, cheapest | Medium | One connection pool and storage footprint per tenant |
| Choose when | SaaS default; many small tenants | Compliance needs schema separation; moderate tenant count | Few large tenants; per-tenant backup/restore, region pinning, or contractual isolation |

The HTTP half of the setup — resolving the requested tenant, validating membership, establishing the current tenant — is **identical in all three cases**. Only the persistence half differs.

---

## Shared setup (all three cases)

### 1. Install packages

```shell
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy # case 1 only
dotnet add package Athenex.Whimclean.Api.AspNetCore.Multitenancy
dotnet add package Athenex.Whimclean.Api.AspNetCore     # Minimal APIs; or Api.FastEndpoints for the FastEndpoints adapter
```

Lower-level packages (`Domain`, `Application`, `Infrastructure`, `Multitenancy.Abstractions`) arrive transitively.

### 2. Authenticate and resolve the current user

Whimclean stores no users; authenticate with your identity provider and let the claims adapter supply `ICurrentUser<Guid>`:

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://keycloak.example.com/realms/myapp";
        options.Audience = "myapp-api";
    });

builder.Services.AddClaimsCurrentUser<Guid>();
```

When user ids are local rows rather than the token subject, use `AddLocalUserResolution<Guid>()` with an `ILocalUserLookup<Guid>` over the user table instead, and add `app.UseLocalUserResolution<Guid>()` after `UseAuthentication()`.

### 3. Own the tenant model and the membership decision

The `Tenant` and `TenantMembership` entities, their lifecycle, and the membership query are yours. Whimclean only asks one question through `ITenantAccessValidator<TUserId, TTenantId>`:

```csharp
public sealed class TenantMembershipAccessValidator(CatalogDbContext catalog)
    : ITenantAccessValidator<Guid, Guid>
{
    public Task<bool> HasAccessAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        catalog.TenantMemberships.AnyAsync(
            membership =>
                membership.UserId == userId &&
                membership.TenantId == tenantId &&
                membership.IsActive,
            cancellationToken);
}
```

In cases 2 and 3, keep this membership data in a shared **catalog** database/schema — it must be readable before any tenant-scoped connection exists.

### 4. Register the HTTP tenant pipeline

```csharp
builder.Services.AddAspNetCoreMultitenancy<Guid, Guid>();
builder.Services.AddHeaderTenantResolution<Guid>(); // default header X-Tenant-Id
builder.Services.AddScoped<
    ITenantAccessValidator<Guid, Guid>,
    TenantMembershipAccessValidator>();
```

```csharp
app.UseAuthentication();
app.UseTenantResolution<Guid>(); // records the *requested* tenant — no authorization yet
app.UseAuthorization();          // TenantMember policy validates and establishes the *current* tenant
```

When the web tier forwards a tenant slug instead of an id, also call `builder.Services.AddSlugTenantResolution()` (default header `X-Tenant-Slug`), register an `ITenantSlugLookup<Guid>` over the tenant table, and add `app.UseSlugTenantResolution<Guid>()` after `UseTenantResolution<Guid>()`. The slug is consulted only when no requested tenant was recorded, so an explicit `X-Tenant-Id` wins.

Protect tenant endpoints with the supplied policy:

```csharp
app.MapApiGroup().RequireAuthorization(MultitenancyPolicies.TenantMember); // Minimal API group
// or Policies(MultitenancyPolicies.TenantMember) in a FastEndpoints Configure method
```

The requested tenant (an unverified transport claim) and the current tenant (established only after `ITenantAccessValidator` approves) are deliberately separate interfaces — code that consumes `ICurrentTenant<Guid>` never sees an unvalidated value. A host, route, or claim resolver can replace `IRequestedTenantResolver<Guid>` without touching anything below.

### 5. Non-HTTP hosts

Background workers, queue consumers, and seeders have no HTTP resolution. Register the ambient implementation and wrap tenant-scoped work explicitly:

```csharp
services.AddScoped<ICurrentTenant<Guid>, AmbientCurrentTenant<Guid>>();
services.AddScoped<ICurrentUser<Guid>>(_ => new StaticCurrentUser<Guid>(SystemUserId));
```

```csharp
using (AmbientCurrentTenant<Guid>.Begin(tenantId))
{
    // reads are filtered and writes attributed to tenantId here
}
```

The caller vouches for the tenant — `Begin` performs no access validation.

---

## Case 1: Logical isolation (shared schema, `TenantId` column)

Fully supported by the shipped packages. Tenant-owned entities implement `IMultiTenant<Guid>`, and the context derives from `MultiTenantDbContext`:

```csharp
public sealed class Order : AggregateRoot<Guid>, IAuditable<Guid?>, IMultiTenant<Guid>
{
    public Guid TenantId { get; private set; }
    // ...
}

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    TimeProvider timeProvider,
    ICurrentUser<Guid> currentUser,
    IAuditActorResolver<Guid, Guid?> auditActorResolver,
    IDomainEventDispatcher domainEventDispatcher,
    ICurrentTenant<Guid> currentTenant)
    : MultiTenantDbContext<Guid, Guid?, Guid>(
        options, timeProvider, currentUser, auditActorResolver,
        domainEventDispatcher, currentTenant)
{
    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
}
```

```csharp
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddNullableAuditActorResolver<Guid>();
builder.Services.AddDomainEventDispatcher();
builder.Services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(connectionString);
```

What the context enforces:

- `IMultiTenant<Guid>` entities receive the fail-closed named `TenantFilter`: with no current tenant, queries return nothing.
- Every added, modified, or deleted tenant-owned row must belong to the current tenant, and a modified `TenantId` is rejected outright (`TenantAccessViolationException`) — a row can never be reassigned between tenants, not even *into* the current one.
- Administrative queries can disable one filter at a time without dropping the other:

```csharp
var acrossTenants = dbContext.Orders.IgnoreQueryFilters([QueryFilterNames.Tenant]);
var includingDeleted = dbContext.Orders.IncludeSoftDeleted();
```

Migrations: a single `dotnet ef database update` — one schema serves every tenant.

---

## Case 2: Schema isolation (PostgreSQL schema per tenant)

Not packaged yet; the agreed pattern is a **connection interceptor that sets `search_path`** on every opened connection. Do **not** use `HasDefaultSchema` per tenant — that would create one EF model cache entry per tenant.

Because the schema isolates the data, entities do not implement `IMultiTenant<Guid>` and the context derives from plain `AuditableDbContext<Guid, Guid?>` (auditing, soft delete, and domain-event dispatch still apply).

### The interceptor

```csharp
public sealed class TenantSearchPathInterceptor(ICurrentTenant<Guid> currentTenant)
    : DbConnectionInterceptor
{
    public override void ConnectionOpened(
        DbConnection connection, ConnectionEndEventData eventData) =>
        SetSearchPath(connection);

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        using var command = CreateSearchPathCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void SetSearchPath(DbConnection connection)
    {
        using var command = CreateSearchPathCommand(connection);
        command.ExecuteNonQuery();
    }

    private DbCommand CreateSearchPathCommand(DbConnection connection)
    {
        if (!currentTenant.TryGetTenantId(out var tenantId))
        {
            // Fail closed: without a tenant there is no schema to query.
            throw new CurrentTenantRequiredException();
        }

        // SET does not accept parameters; set_config does, which keeps
        // the schema name out of SQL string concatenation. The name is
        // derived from a Guid, so its charset is safe by construction.
        var command = connection.CreateCommand();
        command.CommandText =
            "SELECT set_config('search_path', $1, false)";
        var parameter = command.CreateParameter();
        parameter.Value = $"tenant_{tenantId:N},public";
        command.Parameters.Add(parameter);
        return command;
    }
}
```

### Register the interceptor

The interceptor needs the scoped `ICurrentTenant`, so register through the provider-neutral method. The `AddPostgreSqlUnitOfWork` shortcut only exposes an optional `NpgsqlDbContextOptionsBuilder` action, not the service provider or the `DbContextOptionsBuilder` that `AddInterceptors` lives on:

```csharp
builder.Services.AddScoped<TenantSearchPathInterceptor>();
builder.Services.AddEntityFrameworkCoreUnitOfWork<ApplicationDbContext>(
    (serviceProvider, options) => options
        .UseNpgsql(connectionString)
        .AddInterceptors(
            serviceProvider.GetRequiredService<TenantSearchPathInterceptor>()));
```

`DbContextOptions` are scoped, so each request builds its options with that request's interceptor instance.

### Rules and caveats

- **Never interpolate an untrusted value into the schema name.** Derive it from the validated current tenant id only (the `tenant_{guid:N}` convention above), never from a header or route value.
- Connection pooling is safe: the interceptor runs on every physical *open*, and Npgsql resets session state (including `search_path`) when a pooled connection is reused.
- Keep shared reference tables in `public`; the `,public` suffix in the search path makes them visible read-through.
- Provisioning a tenant = `CREATE SCHEMA tenant_x` + running migrations against it.

### Migrations (one run per schema)

A packaged `ITenantMigrationRunner` is planned; until then, iterate the schemas yourself. Each schema gets its own `__EFMigrationsHistory`, because the history table follows the `search_path`:

```csharp
foreach (var tenantId in await catalog.GetTenantIdsAsync())
{
    using (AmbientCurrentTenant<Guid>.Begin(tenantId))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        // ExecuteSqlRawAsync, not ExecuteSqlAsync: DDL cannot take the
        // schema name as a parameter. Safe here because the name is
        // derived from the Guid, never from user input.
        await dbContext.Database.ExecuteSqlRawAsync(
            $"CREATE SCHEMA IF NOT EXISTS tenant_{tenantId:N}");
        await dbContext.Database.MigrateAsync();
    }
}
```

Run this from a maintenance host that registers `AmbientCurrentTenant<Guid>` and `StaticCurrentUser<Guid>` (section “Non-HTTP hosts”).

---

## Case 3: Database isolation (database per tenant)

Not packaged yet; the agreed pattern is a **per-request connection resolver** backed by a consumer-owned tenant store. A packaged `ITenantStore` abstraction is planned.

As with case 2, entities do not implement `IMultiTenant<Guid>` and the context derives from `AuditableDbContext<Guid, Guid?>`.

### The tenant connection store

Map tenant → connection string from a shared catalog database. The options action below is synchronous, so serve the lookup from an in-memory snapshot (refresh it on tenant provisioning):

```csharp
public interface ITenantConnectionStore
{
    string GetConnectionString(Guid tenantId);
}
```

### Register the connection resolver

```csharp
builder.Services.AddSingleton<ITenantConnectionStore, CatalogTenantConnectionStore>();
builder.Services.AddEntityFrameworkCoreUnitOfWork<ApplicationDbContext>(
    (serviceProvider, options) =>
    {
        var currentTenant = serviceProvider
            .GetRequiredService<ICurrentTenant<Guid>>();

        if (!currentTenant.TryGetTenantId(out var tenantId))
        {
            // Fail closed: resolving the context without a tenant is a bug,
            // and the API layer maps this to 403 Tenancy.CurrentTenantRequired.
            throw new CurrentTenantRequiredException();
        }

        var store = serviceProvider
            .GetRequiredService<ITenantConnectionStore>();
        options.UseNpgsql(store.GetConnectionString(tenantId));
    });
```

`DbContextOptions` are scoped, so the connection string is chosen per request — after the `TenantMember` policy has established the current tenant.

### Operational caveats

- The catalog (tenants, memberships, connection strings) lives in its own database with its own ordinary context — register it via `AddPostgreSqlContext<CatalogDbContext>` so it never becomes the application `IUnitOfWork`.
- Each tenant database gets its own Npgsql connection pool; with many tenants, cap `Maximum Pool Size` per tenant or the pools will starve the server.
- Never build the connection string from request input — only from the catalog row keyed by the *validated* current tenant.
- Provisioning a tenant = create the database, insert the catalog row, run migrations, refresh the store snapshot.

### Migrations (one run per database)

The connection resolver above already picks the database from the current tenant, so iterate the catalog under an ambient tenant scope and resolve the context from the container:

```csharp
foreach (var tenantId in await catalog.GetTenantIdsAsync())
{
    using (AmbientCurrentTenant<Guid>.Begin(tenantId))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
```

As in case 2, run this from a maintenance host that registers `AmbientCurrentTenant<Guid>` and `StaticCurrentUser<Guid>` (section “Non-HTTP hosts”); migrations dispatch no domain events.

---

## Mixing the models

The cases compose. Two combinations worth knowing:

- **Defense in depth**: keep `IMultiTenant<Guid>` + `MultiTenantDbContext` *on top of* schema or database isolation. The row filter then guards against a misrouted connection at the cost of a redundant `TenantId` column.
- **Tiered tenants**: big tenants on dedicated databases, small ones sharing a logical-isolation database. The HTTP pipeline is identical for both; only the options action branches on the catalog's isolation mode for the tenant.

Start with case 1 unless a requirement says otherwise — it is the cheapest to run and to migrate, and the fail-closed filter plus write validation (including the tenant-reassignment rejection) already gives strong application-level guarantees. Moving a tenant out to its own schema or database later is a data migration, not a code rewrite: the domain model and HTTP pipeline stay unchanged.
