# Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL

The PostgreSQL/Npgsql provider adapter for Whimclean EF Core infrastructure.

## Install

```shell
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL
```

The package supplies Npgsql and depends on the provider-neutral EF Core package.

## Register a business context

Use `AddPostgreSqlDbContext` when the context implements `IUnitOfWork`, such as a context derived from `AuditableDbContext` or `MultiTenantDbContext`:

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL;

services.AddPostgreSqlDbContext<ApplicationDbContext>(
    connectionString,
    postgreSql => postgreSql.MigrationsAssembly(
        "MyApp.Migrations.PostgreSQL"));
```

This registers both `ApplicationDbContext` and `IUnitOfWork` with scoped lifetimes.

## Register an ordinary EF context

Use `AddPostgreSqlContext` for a context that must not become the application unit of work. ASP.NET Core Identity is the common example:

```csharp
services.AddPostgreSqlContext<AuthenticationDbContext>(
    identityConnectionString,
    postgreSql => postgreSql.MigrationsAssembly(
        "MyApp.Identity.Migrations.PostgreSQL"));
```

## Context separation

Identity and business contexts may share one PostgreSQL database, but keep their migration histories and migration commands explicitly identifiable by context. Whimclean does not automatically run migrations.

Multitenancy is provider-neutral. A tenant-aware context still uses `AddPostgreSqlDbContext`; install the separate EF multitenancy package to derive that context from `MultiTenantDbContext`.

This package contains no tenant model, membership management, Identity model, or API behavior.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
