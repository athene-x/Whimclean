# Upgrading to 1.3.0

[ภาษาไทย](upgrading-to-1.3-TH.md)

Every change is additive; existing registrations keep working. These steps replace code the consuming applications had written themselves.

## List queries

Delete the application's `ListQueryParser`, `ListQueryRequest`, `ListQueryRequestValidator` and filter binding, then:

- import `Athenex.Whimclean.Application.Queries` for the parser and `Athenex.Whimclean.Api.FastEndpoints.Queries` for the request and validator;
- replace `AllowFilterFields(dictionary, isValidValue)` with one `AllowFilterField` or `AllowEnumFilterField<TEnum>` call per field;
- remove the `KeepListQueryFiltersWhole` call from `UseFastEndpointsApi`; the adapter applies it.

In the persistence project delete `SortFields`, `LikePattern`, `EnumFilter` and the `Search`, `Page` and `ToPage` extensions, import `Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries` (and `Athenex.Whimclean.Application.Queries` for `EnumFilter`), and replace hand-written `EF.Functions.ILike` predicates with `SearchILike(options.SearchTerm, x => x.Field, ...)` from the PostgreSQL package.

## Current user and tenant resolution

Replace the application's local-user middleware with `AddLocalUserResolution<Guid>` plus an `ILocalUserLookup<Guid>` over the user table, and `UseLocalUserResolution<Guid>()` after authentication. Replace the slug middleware and resolver with `AddSlugTenantResolution`, an `ITenantSlugLookup<Guid>`, and `UseSlugTenantResolution<Guid>()` after `UseTenantResolution<Guid>()` and before `UseAuthorization()`.

## Keycloak

Replace the JWT bearer setup with `AddKeycloakAuthentication(configuration)`; the `Keycloak` section keeps `Authority`, `Audience`, `MetadataAddress` and `RequireHttpsMetadata`. Replace the admin client with `AddKeycloakAdministration(configuration)` and depend on `IKeycloakUserAdministration`; the `KeycloakAdmin` section is unchanged.

## Audit log

Replace the application's auditing types with the package: commands implement `IAuditedCommand` and return an `AuditDescriptor`; event handlers derive from `DomainEventHandler<TEvent>` (from `Athenex.Whimclean.Mediator`) and use `IAuditChangeCollector`; the application keeps only its `IAuditLogWriter<Guid, Guid>` and the EF mapping of `AuditLog<Guid, Guid>`. Register with `AddAuditing<Guid, Guid>()` before `AddUnitOfWorkBehavior()` and `AddHttpAuditRequestContext()` in the host. The entity's columns match the previous shape, so the existing table needs no migration beyond renaming the CLR type in the model.

## Operations

Replace the health, rate limiting, migration switch, options validation and configuration placeholder helpers with `MapHealthEndpoints`, `AddRequestRateLimiting`, `DatabaseMigration.IsRequested` with `MigrateDatabaseAsync<TContext>`, `AddValidatedOptions` and `ConfigurationPlaceholder` from `Athenex.Whimclean.Api.AspNetCore` (namespaces `Operations` and `Configuration`) and the EF Core package (`Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Operations`). Register the database readiness check with `AddDbContextCheck<TContext>(tags: [HealthCheckTags.Ready])` from `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`.

## Email

Register `AddSmtpEmailSender` and send through `IEmailSender`; application notifiers compose an `EmailMessage` instead of talking to `SmtpClient`.
