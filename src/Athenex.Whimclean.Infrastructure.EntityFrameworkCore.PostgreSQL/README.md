# Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL

The PostgreSQL/Npgsql provider adapter for Whimclean EF Core infrastructure.

## Install

```shell
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL
```

The package supplies Npgsql and depends on the provider-neutral EF Core package.

## Register a business context

Use `AddPostgreSqlUnitOfWork` when the context implements `IUnitOfWork`, such as a context derived from `AuditableDbContext` or `MultiTenantDbContext`:

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL;

services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(
    connectionString,
    postgreSql => postgreSql.MigrationsAssembly(
        "MyApp.Migrations.PostgreSQL"));
```

This registers both `ApplicationDbContext` and `IUnitOfWork` with scoped lifetimes.

## Register an ordinary EF context

Use `AddPostgreSqlContext` for a framework-owned context that must not become the application unit of work, such as a separate reporting or integration context:

```csharp
services.AddPostgreSqlContext<ReportingDbContext>(
    reportingConnectionString,
    postgreSql => postgreSql.MigrationsAssembly(
        "MyApp.Reporting.Migrations.PostgreSQL"));
```

## Context separation

Multiple contexts may share one PostgreSQL database, but keep their migration histories and migration commands explicitly identifiable by context. Whimclean does not automatically run migrations.

Multitenancy is provider-neutral. A tenant-aware context still uses `AddPostgreSqlUnitOfWork`; install the separate EF multitenancy package to derive that context from `MultiTenantDbContext`.

This package contains no tenant model, membership management, or API behavior.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
