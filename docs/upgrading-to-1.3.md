# Upgrading to 1.3.0

[ภาษาไทย](upgrading-to-1.3-TH.md)

Version 1.3.0 is additive: every registration from 1.2.0 keeps working. The steps below replace code that consuming applications had written themselves with the equivalents the packages provide.

## Contents

- [API packages](#api-packages)
- [List queries](#list-queries)
- [Current user and tenant resolution](#current-user-and-tenant-resolution)
- [Keycloak](#keycloak)
- [Audit log](#audit-log)
- [Operations](#operations)
- [Email](#email)

## API packages

`Athenex.Whimclean.Api.AspNetCore` is the primary API package, built on ASP.NET Core Minimal APIs. `Athenex.Whimclean.Api.FastEndpoints` is a thin adapter over it.

### FastEndpoints applications

Keep `AddFastEndpointsApi` and `UseFastEndpointsApi`. Two behaviors to be aware of:

- Endpoint discovery runs with `IncludeAbstractValidators` enabled, so every `AbstractValidator<T>` descendant in the scanned assemblies is treated as a request validator. If an assembly contains validators that are not request validators, limit the scan:

  ```csharp
  builder.Services.AddFastEndpointsApi(
      configureOpenApi: null,
      configureDiscovery: discovery => discovery.Assemblies = [typeof(Program).Assembly]);
  ```

- A 400 response that ends without a body receives the `ApiErrorResponse` body `Request.Invalid`, in the same way 401, 403 and 404 responses do. Responses that write their own body, such as validation failures, are untouched.

### Minimal API applications

Register and map the API through the primary package:

```csharp
builder.Services.AddMinimalApi();
builder.Services.AddApiValidatorsFromAssemblyContaining<Program>();

app.UseMinimalApi();

var api = app.MapApiGroup();
api.MapGet("orders", async ([AsParameters] ListOrdersRequest request, IOrderQueries queries, CancellationToken ct) =>
    Results.Ok(await queries.ListAsync(request.ToListQueryOptions(), ct)));
```

`MapApiGroup` applies request validation to the `api/v1` group; `WithRequestValidation` applies it to endpoints mapped elsewhere. Validation failures use the same `Validation.Failed` body as FastEndpoints.

## List queries

Delete the application's own `ListQueryParser`, `ListQueryRequest`, `ListQueryRequestValidator` and filter binding, then:

1. Import `Athenex.Whimclean.Application.Queries` for the parser and `Athenex.Whimclean.Api.AspNetCore.Queries` for the request and validator.
2. Replace `AllowFilterFields(dictionary, isValidValue)` with one `AllowFilterField` or `AllowEnumFilterField<TEnum>` call per field.
3. Replace any `Resolve<T>()` call inside a validator with constructor injection. The validator derives from FluentValidation's `AbstractValidator<TRequest>`, not FastEndpoints' `Validator<TRequest>`.
4. Remove the `KeepListQueryFiltersWhole` call from `UseFastEndpointsApi`; the adapter applies it. Minimal APIs need no binding: `[AsParameters]` keeps each repeated `filters` value whole.

In the persistence project, delete `SortFields`, `LikePattern`, `EnumFilter` and the `Search`, `Page` and `ToPage` extensions. Import `Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries` (and `Athenex.Whimclean.Application.Queries` for `EnumFilter`), and replace hand-written `EF.Functions.ILike` predicates with `SearchILike(options.SearchTerm, x => x.Field, ...)` from the PostgreSQL package.

## Current user and tenant resolution

- Replace the application's local-user middleware with `AddLocalUserResolution<Guid>` plus an `ILocalUserLookup<Guid>` over the user table, and `UseLocalUserResolution<Guid>()` after authentication.
- Replace the slug middleware and resolver with `AddSlugTenantResolution`, an `ITenantSlugLookup<Guid>`, and `UseSlugTenantResolution<Guid>()` after `UseTenantResolution<Guid>()` and before `UseAuthorization()`.

## Keycloak

- Replace the JWT bearer setup with `AddKeycloakAuthentication(configuration)`. The `Keycloak` section keeps `Authority`, `Audience`, `MetadataAddress` and `RequireHttpsMetadata`.
- Replace the admin client with `AddKeycloakAdministration(configuration)` and depend on `IKeycloakUserAdministration`. The `KeycloakAdmin` section is unchanged.

## Audit log

Replace the application's auditing types with the package:

- commands implement `IAuditedCommand` and return an `AuditDescriptor`;
- event handlers derive from `DomainEventHandler<TEvent>` (from `Athenex.Whimclean.Mediator`) and use `IAuditChangeCollector`;
- the application keeps only its `IAuditLogWriter<Guid, Guid>` and the EF mapping of `AuditLog<Guid, Guid>`.

Register with `AddAuditing<Guid, Guid>()` before `AddUnitOfWorkBehavior()`, and `AddHttpAuditRequestContext()` in the host. The entity's columns match the previous shape, so the existing table needs no migration beyond renaming the CLR type in the model.

## Operations

Replace the application's helpers with the package equivalents:

| Application helper | Replacement | Package |
| --- | --- | --- |
| Health endpoints | `MapHealthEndpoints` | `Api.AspNetCore` (`Operations`) |
| Rate limiting | `AddRequestRateLimiting` | `Api.AspNetCore` (`Operations`) |
| Migration switch | `DatabaseMigration.IsRequested` with `MigrateDatabaseAsync<TContext>` | `Infrastructure.EntityFrameworkCore` (`Operations`) |
| Options validation | `AddValidatedOptions` | `Api.AspNetCore` (`Configuration`) |
| Configuration placeholder | `ConfigurationPlaceholder` | `Api.AspNetCore` (`Configuration`) |

Register the database readiness check with `AddDbContextCheck<TContext>(tags: [HealthCheckTags.Ready])` from `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`.

## Email

Register `AddSmtpEmailSender` and send through `IEmailSender`. Application notifiers compose an `EmailMessage` instead of talking to `SmtpClient`.
