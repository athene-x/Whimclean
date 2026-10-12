# Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL

[ภาษาไทย](README-TH.md)

The PostgreSQL/Npgsql provider adapter for Whimclean EF Core infrastructure.

## Install

```shell
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL
```

The package supplies `Npgsql.EntityFrameworkCore.PostgreSQL` and depends on the provider-neutral EF Core package.

## Register a business context

Use `AddPostgreSqlUnitOfWork` when the context implements `IUnitOfWork`, such as a context derived from `AuditableDbContext` or `MultiTenantDbContext`:

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL;

services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(
    connectionString,
    postgreSql => postgreSql.MigrationsAssembly(
        "MyApp.Migrations.PostgreSQL"));
```

This calls `UseNpgsql` with the connection string and registers both `ApplicationDbContext` and `IUnitOfWork` with scoped lifetimes. The second argument is an optional `Action<NpgsqlDbContextOptionsBuilder>`. A null or whitespace connection string throws `ArgumentException`.

## Register an ordinary EF context

Use `AddPostgreSqlContext` for a framework-owned context that must not become the application unit of work, such as a separate reporting or integration context:

```csharp
services.AddPostgreSqlContext<ReportingDbContext>(
    reportingConnectionString,
    postgreSql => postgreSql.MigrationsAssembly(
        "MyApp.Reporting.Migrations.PostgreSQL"));
```

Only the DbContext is registered; nothing else.

## Context separation

Multiple contexts may share one PostgreSQL database, but keep their migration histories and migration commands explicitly identifiable by context. Whimclean never runs migrations on its own; the EF Core package applies them only when the application is started with the `--migrate` switch handled by `DatabaseMigration`.

Multitenancy is provider-neutral. A tenant-aware context still uses `AddPostgreSqlUnitOfWork`; install the separate EF multitenancy package to derive that context from `MultiTenantDbContext`.

This package contains no tenant model, membership management, or API behavior.

## Case-insensitive search

`SearchILike` narrows a query to rows where any of the given string members contains the search term, using `EF.Functions.ILike` with `%`, `_` and `\` escaped through `LikePattern`; a null term leaves the query unchanged. The pattern is sent as a query parameter, and an empty field list throws `ArgumentException`:

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL.Queries;

query = query.SearchILike(options.SearchTerm, user => user.Email, user => user.DisplayName);
```

Repository and full architecture guide: https://github.com/athene-x/Whimclean
