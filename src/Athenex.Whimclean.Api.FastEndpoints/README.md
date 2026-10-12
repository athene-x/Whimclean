# Athenex.Whimclean.Api.FastEndpoints

[ภาษาไทย](README-TH.md)

FastEndpoints adapter for [`Athenex.Whimclean.Api.AspNetCore`](https://github.com/athene-x/Whimclean/blob/main/src/Athenex.Whimclean.Api.AspNetCore/README.md), the primary Whimclean API package. Install it in applications that write their endpoints with FastEndpoints.

The adapter owns only what is specific to FastEndpoints:

- FastEndpoints registration and endpoint discovery, including discovery of plain FluentValidation validators;
- the FastEndpoints OpenAPI document;
- rendering of FastEndpoints validation failures as the shared error body;
- whole-value binding of `ListQueryRequest` filters.

Everything else comes from the primary package and behaves identically in both styles: the error body and exception mappings, status-code responses, `Result` mapping, the `ListQueryRequest` contract and its validator, current-user resolution, health endpoints, rate limiting and validated options.

## Contents

- [Installation](#installation)
- [Quick start](#quick-start)
- [Registration reference](#registration-reference)
- [OpenAPI](#openapi)
- [Validation](#validation)
- [List endpoints](#list-endpoints)
- [Error responses](#error-responses)

## Installation

```shell
dotnet add package Athenex.Whimclean.Api.FastEndpoints
```

The package references `Athenex.Whimclean.Api.AspNetCore`, `FastEndpoints` and `FastEndpoints.OpenApi`.

## Quick start

```csharp
using Athenex.Whimclean.Api.FastEndpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFastEndpointsApi();

var app = builder.Build();

app.UseAuthentication();   // when authentication is configured
app.UseAuthorization();    // when authorization is configured
app.UseFastEndpointsApi();

app.Run();
```

With this setup:

- every endpoint lives under `/api/v1`;
- validators are discovered and run before the endpoint, and a failure answers 400 `Validation.Failed`;
- every failure, from a thrown exception to an empty 404, answers with the shared error body described in [Error responses](#error-responses);
- repeated `filters` query values of a `ListQueryRequest` are bound whole.

## Registration reference

| Call | Applies to | What it does |
| --- | --- | --- |
| `AddFastEndpointsApi(configureOpenApi)` | `IServiceCollection` | Registers FastEndpoints with `IncludeAbstractValidators` enabled, the shared error handling from `AddApiErrorHandling`, and the FastEndpoints OpenAPI document. The optional delegate customizes `DocumentOptions`. |
| `AddFastEndpointsApi(configureOpenApi, configureDiscovery)` | `IServiceCollection` | Same, with a hook that adjusts `EndpointDiscoveryOptions` after the adapter's settings are applied. |
| `UseFastEndpointsApi(routePrefix, configure)` | `IApplicationBuilder` | `UseApiErrorHandling` plus `UseFastEndpoints` with the route prefix (default `api/v1`), the validation response builder and the filter binding. The optional delegate adjusts the FastEndpoints `Config` afterwards. |

Customizing the pipeline:

```csharp
app.UseFastEndpointsApi(
    routePrefix: "api/v2",
    configure: options =>
    {
        options.Security.PermissionsClaimType = "permission";
    });
```

## OpenAPI

Configure the FastEndpoints document during registration:

```csharp
builder.Services.AddFastEndpointsApi(document =>
{
    document.Title = "Orders API";
});
```

## Validation

### Discovery

FastEndpoints pairs each request DTO with its validator at startup. The adapter enables `IncludeAbstractValidators`, so a validator may derive from FastEndpoints' `Validator<TRequest>` or directly from FluentValidation's `AbstractValidator<TRequest>`. This is what allows validators derived from the primary package's `ListQueryRequestValidator<TRequest>` to be discovered.

Because every `AbstractValidator<T>` descendant in the scanned assemblies is discovered, limit the scan when an assembly contains validators that are not request validators:

```csharp
builder.Services.AddFastEndpointsApi(
    configureOpenApi: null,
    configureDiscovery: discovery => discovery.Assemblies = [typeof(Program).Assembly]);
```

A validator that derives from `AbstractValidator<TRequest>` cannot use FastEndpoints' `Resolve<T>()`; inject dependencies through the constructor instead.

### Validation response

Failures are rendered through the primary package's `ValidationHttpMapper`, so the body matches a Minimal API exactly:

```json
{
  "code": "Validation.Failed",
  "description": "One or more validation errors occurred.",
  "errors": [
    { "code": "NotEmptyValidator", "description": "'Customer Id' must not be empty.", "field": "customerId" }
  ]
}
```

## List endpoints

Derive the request from the primary package's `ListQueryRequest` and the validator from `ListQueryRequestValidator<TRequest>`. FastEndpoints binds the request from the query string, discovers the validator and runs it before the endpoint:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Queries;
using Athenex.Whimclean.Application.Pagination;
using Athenex.Whimclean.Application.Queries;
using FastEndpoints;

public sealed class ListTenantsRequest : ListQueryRequest;

public sealed class ListTenantsRequestValidator : ListQueryRequestValidator<ListTenantsRequest>
{
    public ListTenantsRequestValidator()
    {
        AllowSortFields("slug", "name", "status");
        AllowEnumFilterField<TenantStatus>("status");
        AllowFilterField("createdAt", FilterOperator.GreaterThanOrEqual, FilterOperator.LessThan);
    }
}

public sealed class ListTenantsEndpoint(ITenantQueries queries)
    : Endpoint<ListTenantsRequest, OffsetPagedResult<TenantResponse>>
{
    public override void Configure() => Get("tenants");

    public override async Task HandleAsync(ListTenantsRequest request, CancellationToken ct) =>
        await Send.OkAsync(await queries.ListAsync(request.ToListQueryOptions(), ct), ct);
}
```

The query-string parameters, validation rules and allow-list methods are documented in the primary package's [List endpoints](https://github.com/athene-x/Whimclean/blob/main/src/Athenex.Whimclean.Api.AspNetCore/README.md#list-endpoints) section.

`UseFastEndpointsApi` registers `ListQueryBinding.KeepListQueryFiltersWhole`, which binds each repeated `filters` value as-is so that a comma-separated `in` value (`status:in:draft,published`) stays in one filter. Call it yourself only when configuring `UseFastEndpoints` directly.

## Error responses

Every failure response uses the primary package's body:

```json
{
  "code": "Order.NotFound",
  "description": "The order was not found.",
  "errors": null
}
```

| Condition | HTTP | Code |
| --- | ---: | --- |
| `CurrentUserRequiredException` | 401 | `Authentication.CurrentUserRequired` |
| `PersistenceConcurrencyException` | 409 | `Persistence.ConcurrentUpdate` |
| FastEndpoints validation failure | 400 | `Validation.Failed` |
| Response ended with 400 and no body | 400 | `Request.Invalid` |
| Response ended with 401 and no body | 401 | `Authentication.Unauthorized` |
| Response ended with 403 and no body | 403 | `Authorization.Forbidden` |
| Response ended with 404 and no body | 404 | `Resource.NotFound` |
| Any other exception | 500 | `Server.UnexpectedError` |

Unexpected exception details are logged and never returned to the client.

### Returning a domain error from an endpoint

```csharp
using Athenex.Whimclean.Api.AspNetCore.Errors;

var result = await handler.HandleAsync(request, ct);

if (result.IsFailure)
{
    await Send.ResultAsync(result.Error.ToHttpResult());
    return;
}
```

`ErrorType` maps to status codes as in the primary package: `Validation` 400, `Unauthorized` 401, `Forbidden` 403, `NotFound` 404, `Conflict` 409, `Failure` 500.

### Adding an exception mapping

Register an `IApiExceptionMapper`; see [Adding an exception mapping](https://github.com/athene-x/Whimclean/blob/main/src/Athenex.Whimclean.Api.AspNetCore/README.md#adding-an-exception-mapping) in the primary package. Mappers are probed in registration order and the first match wins.

---

Repository and architecture guide: https://github.com/athene-x/Whimclean
