# Athenex.Whimclean.Api.AspNetCore

[ภาษาไทย](README-TH.md)

The primary API package for Whimclean applications. It targets ASP.NET Core Minimal APIs and provides:

- one-call registration of error handling and an OpenAPI document;
- request validation with FluentValidation, applied to a route group or to individual endpoints;
- the `ListQueryRequest` contract for paginated, sortable and filterable list endpoints;
- a single error response shape for exceptions, empty status-code responses, validation failures and domain `Result` failures;
- current-user resolution from claims or from a local user table;
- health endpoints, per-caller rate limiting and validated options for the host.

Applications written with FastEndpoints install [`Athenex.Whimclean.Api.FastEndpoints`](https://github.com/athene-x/Whimclean/blob/main/src/Athenex.Whimclean.Api.FastEndpoints/README.md) instead. The adapter builds on this package, so both styles share the same contracts and responses.

## Contents

- [Installation](#installation)
- [Quick start](#quick-start)
- [Registration reference](#registration-reference)
- [OpenAPI](#openapi)
- [Request validation](#request-validation)
- [List endpoints](#list-endpoints)
- [Error responses](#error-responses)
- [Mapping results to HTTP](#mapping-results-to-http)
- [Current user](#current-user)
- [Operations](#operations)
- [Validated options](#validated-options)

## Installation

```shell
dotnet add package Athenex.Whimclean.Api.AspNetCore
```

The package depends on `Athenex.Whimclean.Application`, `FluentValidation` and `Microsoft.AspNetCore.OpenApi`.

## Quick start

```csharp
using Athenex.Whimclean.Api.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalApi();
builder.Services.AddApiValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

app.UseAuthentication();   // when authentication is configured
app.UseAuthorization();    // when authorization is configured
app.UseMinimalApi();

var api = app.MapApiGroup();
api.MapGet("orders", () => Results.Ok());

app.Run();
```

With this setup:

- every endpoint mapped on `api` lives under `/api/v1`;
- a handler argument whose type has a registered validator is validated before the handler runs;
- every failure, from a thrown exception to an empty 404, answers with the shared error body described in [Error responses](#error-responses);
- the OpenAPI document is served at `/openapi/v1.json`.

## Registration reference

| Call | Applies to | What it does |
| --- | --- | --- |
| `AddMinimalApi(configureOpenApi)` | `IServiceCollection` | Registers `AddApiErrorHandling` and an OpenAPI document. The optional delegate customizes `OpenApiOptions`. |
| `AddApiValidatorsFromAssembly(assembly, lifetime)` | `IServiceCollection` | Registers every concrete `IValidator<T>` in the assembly under its interface. Scoped by default, through `TryAdd`, so an explicit registration wins. Abstract and open generic validators are skipped. |
| `AddApiValidatorsFromAssemblyContaining<T>(lifetime)` | `IServiceCollection` | Same, for the assembly that contains `T`. |
| `AddApiErrorHandling()` | `IServiceCollection` | The error handling alone: the `IApiExceptionMapper` chain, the `ApiExceptionHandler` and ProblemDetails support. |
| `UseMinimalApi(openApiRoutePattern)` | `WebApplication` | `UseApiErrorHandling` plus the OpenAPI endpoint. Pass `null` to leave the document unmapped. |
| `UseApiErrorHandling()` | `IApplicationBuilder` | Exception handling and bodies for 400, 401, 403 and 404 responses that end without one. |
| `MapApiGroup(routePrefix)` | `IEndpointRouteBuilder` | Creates the route group (default `api/v1`) with request validation applied. |
| `WithRequestValidation()` | endpoint or group | Applies request validation to endpoints mapped outside the group. |

## OpenAPI

`AddMinimalApi` registers the document; `UseMinimalApi` maps it at `/openapi/{documentName}.json`.

Customize the document through `OpenApiOptions`:

```csharp
builder.Services.AddMinimalApi(openApi =>
    openApi.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Orders API";
        return Task.CompletedTask;
    }));
```

To expose the document only in development, leave it unmapped and map it yourself:

```csharp
app.UseMinimalApi(openApiRoutePattern: null);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
```

## Request validation

### How it works

1. `MapApiGroup` (or `WithRequestValidation`) attaches an endpoint filter factory to the group or endpoint.
2. At startup, the factory inspects each handler once and remembers the parameters whose type has an `IValidator<T>` registered. Endpoints with no such parameter run unchanged.
3. Per request, the remembered arguments are validated in parameter order. The first failure short-circuits the handler with 400 `Validation.Failed`.

### Writing a validator

A validator is an ordinary FluentValidation `AbstractValidator<T>`; `AddApiValidatorsFromAssembly` registers it. Dependencies come through the constructor.

```csharp
using FluentValidation;

public sealed record CreateOrderRequest(string CustomerId, decimal Total);

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(request => request.CustomerId).NotEmpty();
        RuleFor(request => request.Total).GreaterThan(0);
    }
}

api.MapPost("orders", (CreateOrderRequest request) => Results.Created());
```

### Validation response

```json
{
  "code": "Validation.Failed",
  "description": "One or more validation errors occurred.",
  "errors": [
    { "code": "NotEmptyValidator", "description": "'Customer Id' must not be empty.", "field": "customerId" },
    { "code": "GreaterThanValidator", "description": "'Total' must be greater than '0'.", "field": "total" }
  ]
}
```

Each entry carries the FluentValidation error code, the message and the property name in camelCase. A failure that is not tied to a property has `field: null`.

### Validating by hand

`ValidationHttpMapper` builds the same response from any FluentValidation result:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Errors;

var result = await validator.ValidateAsync(request, cancellationToken);
if (!result.IsValid)
{
    return result.ToHttpResult();   // 400 with the body above
}
```

`failures.ToApiErrorResponse()` returns the body only, for cases where the status code is set elsewhere.

## List endpoints

`ListQueryRequest` is the query-string contract shared by offset-paginated list endpoints:

| Parameter | Meaning | Default |
| --- | --- | --- |
| `searchTerm` | Free text matched by the endpoint's search fields | none |
| `sort` | Comma-separated field names, `-` prefix for descending | none |
| `filters` | Repeated `field:operator:value` entries | none |
| `page` | One-based page number | `1` |
| `size` | Items per page | `10` |

`ListQueryParser` in `Athenex.Whimclean.Api.AspNetCore.Queries` turns these raw values into `ListQueryOptions`. `sort` is a comma-separated list of field names with a `-` prefix for descending; each `filters` entry is `field:operator:value` with the operators `eq`, `ne`, `gt`, `ge`, `lt`, `le`, `cn` (contains), `sw`, `ew` and `in` (comma-separated values); operator codes are case-insensitive. `Parse`, `ParseSort` and `ParseFilters` throw `FormatException` on bad input; `TryParseFilter` and `TryParseSort` report a readable error without throwing, which `ListQueryRequestValidator<TRequest>` uses.

### Defining the request and validator

Derive a request per endpoint, derive its validator from `ListQueryRequestValidator<TRequest>`, and declare the fields the endpoint supports:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Queries;
using Athenex.Whimclean.Application.Queries;

public sealed class ListOrdersRequest : ListQueryRequest;

public sealed class ListOrdersRequestValidator : ListQueryRequestValidator<ListOrdersRequest>
{
    public ListOrdersRequestValidator()
    {
        AllowSortFields("createdAt", "total");
        AllowEnumFilterField<OrderStatus>("status");
        AllowFilterField("createdAt", FilterOperator.GreaterThanOrEqual, FilterOperator.LessThan);
    }
}
```

### Mapping the endpoint

Bind the request with `[AsParameters]`. The group's request validation runs the validator before the handler, so `ToListQueryOptions()` only ever sees valid input:

```csharp
api.MapGet("orders", async (
    [AsParameters] ListOrdersRequest request,
    IOrderQueries queries,
    CancellationToken cancellationToken) =>
    Results.Ok(await queries.ListAsync(request.ToListQueryOptions(), cancellationToken)));
```

Minimal API binding keeps each repeated `filters` value whole, so a comma-separated `in` value such as `status:in:draft,published` stays in one filter.

### Validation rules

The base validator rejects:

- malformed `sort` and `filters` values;
- `page` below 1, and `size` outside 1 to `ListQueryOptions.MaximumSize`;
- more than `ListQueryOptions.MaximumFilters` filters;
- a `searchTerm` longer than `ListQueryOptions.MaximumSearchLength`.

The allow-lists add endpoint-specific rules:

| Method | Effect |
| --- | --- |
| `AllowSortFields(params string[] fields)` | Only the named fields may appear in `sort`. |
| `AllowFilterField(field, params operators)` | The field may be filtered with the given operators (`Equal` and `In` when none are given) and any value. |
| `AllowFilterField(field, isValidValue, params operators)` | Same, accepting only values for which the predicate returns `true`. |
| `AllowEnumFilterField<TEnum>(field, params operators)` | Same, accepting the members of `TEnum` case-insensitively. |

Once any filter field is allowed, filters naming other fields are rejected. Without an allow-list every field passes and the query ignores what it does not know. Rejections name the offending field, operator or value, for example `Sort field 'email' is not supported.`

## Error responses

Every failure response uses the same body:

```json
{
  "code": "Resource.NotFound",
  "description": "The requested resource was not found.",
  "errors": null
}
```

| Condition | HTTP | Code |
| --- | ---: | --- |
| `CurrentUserRequiredException` | 401 | `Authentication.CurrentUserRequired` |
| `PersistenceConcurrencyException` | 409 | `Persistence.ConcurrentUpdate` |
| Request validation failure | 400 | `Validation.Failed` |
| Response ended with 400 and no body (for example a query value that could not be bound) | 400 | `Request.Invalid` |
| Response ended with 401 and no body | 401 | `Authentication.Unauthorized` |
| Response ended with 403 and no body | 403 | `Authorization.Forbidden` |
| Response ended with 404 and no body | 404 | `Resource.NotFound` |
| Any other exception | 500 | `Server.UnexpectedError` |

Unexpected exception details are logged and never returned to the client. A request aborted by the client produces no body and is logged at debug level.

### Adding an exception mapping

Implement `IApiExceptionMapper` and register it. Mappers are probed in registration order and the first match wins; the built-in `DefaultApiExceptionMapper` is registered with `TryAddEnumerable`, so additional mappers never replace it.

```csharp
using Athenex.Whimclean.Api.AspNetCore.Errors;
using Microsoft.AspNetCore.Http;

public sealed class OrderApiExceptionMapper : IApiExceptionMapper
{
    public bool TryMap(Exception exception, out ApiExceptionMapping mapping)
    {
        if (exception is OrderLockedException)
        {
            mapping = new ApiExceptionMapping(
                StatusCodes.Status409Conflict,
                new ApiErrorResponse("Order.Locked", "The order is currently locked."));
            return true;
        }

        mapping = default;
        return false;
    }
}

builder.Services.AddSingleton<IApiExceptionMapper, OrderApiExceptionMapper>();
```

`Athenex.Whimclean.Api.AspNetCore.Multitenancy` contributes its tenant mappings through the same extension point.

## Mapping results to HTTP

`ToHttpResult` turns a domain `Result` or `Error` into an `IResult` with the shared body:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Errors;

api.MapGet("orders/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetOrderQuery(id));
    return result.ToHttpResult(order => Results.Ok(order));
});
```

| `ErrorType` | HTTP |
| --- | ---: |
| `Validation` | 400 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Failure` | 500 |

## Current user

Whimclean does not store users or credentials. Authenticate with any provider that populates `HttpContext.User` (Keycloak, Microsoft Entra ID, Auth0, ASP.NET Core Identity) and choose how the authenticated principal becomes the `ICurrentUser<TUserId>` that the auditing write boundary consumes.

### From claims

`AddClaimsCurrentUser<TUserId>` reads the user id from a claim on the principal:

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

The default claim type is `ClaimTypes.NameIdentifier`, which JWT bearer authentication maps the `sub` claim to. When inbound claim mapping is disabled (`options.MapInboundClaims = false`), read the raw claim instead:

```csharp
builder.Services.AddClaimsCurrentUser<Guid>(options => options.ClaimType = "sub");
```

`TUserId` must implement `IParsable<TUserId>`. Registration uses `TryAddScoped`, so an application-supplied `ICurrentUser<TUserId>` (for example a background-job system user) wins.

### From a local user table

When the application keeps its own user rows and the token carries only the provider's subject, resolve the local id per request:

```csharp
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Api.AspNetCore.Context;
using Microsoft.EntityFrameworkCore;

public sealed class LocalUserLookup(ApplicationDbContext dbContext) : ILocalUserLookup<Guid>
{
    public async ValueTask<Guid> FindByExternalSubjectAsync(string externalSubject, CancellationToken ct = default) =>
        await dbContext.Users
            .Where(user => user.ExternalSubject == externalSubject)
            .Select(user => user.Id)
            .SingleOrDefaultAsync(ct);
}

builder.Services.AddLocalUserResolution<Guid>(options => options.ClaimType = "sub");
builder.Services.AddScoped<ILocalUserLookup<Guid>, LocalUserLookup>();

app.UseAuthentication();
app.UseLocalUserResolution<Guid>();
```

`AddLocalUserResolution` registers the scoped `RequestCurrentUser<TUserId>` as `ICurrentUser<TUserId>` (via `TryAddScoped`) and as `ICurrentUserInitializer<TUserId>`, so a startup bootstrap can attribute its writes after resolving its own user. The middleware reads the subject from the claim named by `ClaimsCurrentUserOptions.ClaimType` and calls the lookup. A default (empty) identifier means no local user: the current user stays unset and the write boundary rejects writes. Place the middleware after authentication and before anything that reads the current user, such as tenant resolution and authorization.

## Operations

```csharp
using Athenex.Whimclean.Api.AspNetCore.Operations;

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(tags: [HealthCheckTags.Ready]);
builder.Services.AddRequestRateLimiting(builder.Configuration);

app.UseAuthentication();
app.UseRateLimiter();
app.MapHealthEndpoints();
```

### Health endpoints

`MapHealthEndpoints` maps two anonymous endpoints that are exempt from rate limiting:

| Path | Behavior |
| --- | --- |
| `/health/live` | Answers as soon as the process serves requests; runs no checks. |
| `/health/ready` | Runs the checks tagged `HealthCheckTags.Ready`. |

Both paths can be changed through the `livePath` and `readyPath` parameters. `AddDbContextCheck` comes from `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`.

### Rate limiting

`AddRequestRateLimiting` adds a global fixed window per caller, keyed by the `sub` claim (`user:{sub}`) or, for anonymous requests, the remote address (`ip:{address}`). Rejections answer 429.

| Setting (`RateLimiting` section) | Default |
| --- | ---: |
| `PermitLimit` | 300 |
| `WindowSeconds` | 60 |

The options are checked with `RequestRateLimitOptions.Validate()` at registration: a permit limit or window that is not positive throws `InvalidOperationException` naming the setting. An overload takes a `RequestRateLimitOptions` instance instead of configuration.

## Validated options

`AddValidatedOptions` binds an options class to a configuration section and validates it when the host starts, so a misconfigured deployment fails at boot with a message naming the setting:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Configuration;

builder.Services.AddValidatedOptions<EmailOptions>(
    builder.Configuration,
    "Email",
    options => string.IsNullOrWhiteSpace(options.Host) ? "Email:Host is required." : null);
```

The validation delegate returns a failure message, or `null` when the options are valid.

`ConfigurationPlaceholder.IsUnset` recognizes the `<set-via-user-secrets>` marker (`ConfigurationPlaceholder.Value`) committed in `appsettings.json` for values that must come from user secrets or the environment. A missing or blank value also counts as unset.

---

Repository and architecture guide: https://github.com/athene-x/Whimclean
