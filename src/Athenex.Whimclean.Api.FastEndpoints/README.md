# Athenex.Whimclean.Api.FastEndpoints

[ภาษาไทย](README-TH.md)

FastEndpoints adapter for the Whimclean API conventions: endpoint registration, OpenAPI, and validation responses. The error contract, exception handling, status-code responses, and result mapping live in `Athenex.Whimclean.Api.AspNetCore` (referenced by this package), so they stay identical if an application later switches to Minimal APIs.

## Install

```shell
dotnet add package Athenex.Whimclean.Api.FastEndpoints
```

## Register services and middleware

```csharp
using Athenex.Whimclean.Api.FastEndpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFastEndpointsApi();

var app = builder.Build();

app.UseAuthentication();       // when authentication is configured
app.UseAuthorization();        // when authorization is configured
app.UseFastEndpointsApi();

app.Run();
```

`AddFastEndpointsApi` registers FastEndpoints, the shared error handling from `AddApiErrorHandling`, and an OpenAPI document. `UseFastEndpointsApi` applies the default `api/v1` route prefix, exception handling, consistent 401/403/404 responses for endpoints that end without a body, FastEndpoints validation mapping, whole-value binding of list query filters, and endpoint discovery.

Customize the route prefix and FastEndpoints configuration when needed:

```csharp
app.UseFastEndpointsApi(
    routePrefix: "api/v2",
    configure: options =>
    {
        options.Security.PermissionsClaimType = "permission";
    });
```

Configure the generated OpenAPI document during service registration:

```csharp
builder.Services.AddFastEndpointsApi(document =>
{
    document.Title = "Orders API";
});
```

## Map application errors

Convert a failed Whimclean `Error` into the standard HTTP response:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Errors;

var result = await handler.HandleAsync(request, cancellationToken);

if (result.IsFailure)
{
    await Send.ResultAsync(result.Error.ToHttpResult());
    return;
}
```

Mappings are validation → 400, unauthorized → 401, forbidden → 403, not found → 404, conflict → 409, and general failure → 500.

Responses use this shape:

```json
{
  "code": "Order.NotFound",
  "description": "The order was not found.",
  "errors": null
}
```

FastEndpoints validation failures answer 400 with code `Validation.Failed` and populate `errors` with the code, description, and camel-cased field of each failure.

## Built-in exception mappings

- `CurrentUserRequiredException` → 401 `Authentication.CurrentUserRequired`
- `PersistenceConcurrencyException` → 409 `Persistence.ConcurrentUpdate`
- unexpected exceptions → 500 `Server.UnexpectedError`

Unexpected details are logged but never returned to the client.

## Add a product-specific exception mapping

```csharp
using Athenex.Whimclean.Api.AspNetCore.Errors;
using Microsoft.AspNetCore.Http;

public sealed class OrderApiExceptionMapper : IApiExceptionMapper
{
    public bool TryMap(
        Exception exception,
        out ApiExceptionMapping mapping)
    {
        if (exception is OrderLockedException)
        {
            mapping = new ApiExceptionMapping(
                StatusCodes.Status409Conflict,
                new ApiErrorResponse(
                    "Order.Locked",
                    "The order is currently locked."));
            return true;
        }

        mapping = default;
        return false;
    }
}

services.AddSingleton<IApiExceptionMapper, OrderApiExceptionMapper>();
```

Mappers are probed in registration order and the first match wins. The optional ASP.NET Core multitenancy package contributes tenant mappings through the same extension point.

## List endpoints

Derive the request from `ListQueryRequest` to get `searchTerm`, `sort`, `filters`, `page` and `size` bound from the query string, and derive a validator from `ListQueryRequestValidator<TRequest>` to declare what the endpoint supports:

```csharp
using Athenex.Whimclean.Api.FastEndpoints.Queries;
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

The base validator rejects malformed sort and filter values, page and size out of range (`page` ≥ 1, `size` between 1 and `ListQueryOptions.MaximumSize`), more than `ListQueryOptions.MaximumFilters` filters, and an over-long search term; the allow-lists reject unknown fields, operators and values with a message naming them. `AllowFilterField` and `AllowEnumFilterField` default to the `Equal` and `In` operators when none are given; `AllowEnumFilterField<TEnum>` accepts enum member names case-insensitively. Without an allow-list every field passes and the query ignores what it does not know. `ToListQueryOptions` parses the request into `ListQueryOptions`; run the validator first so malformed input surfaces as a 400 rather than an exception. `UseFastEndpointsApi` registers the binding (`ListQueryBinding.KeepListQueryFiltersWhole`) that keeps a comma-separated `in` value in one filter.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
