# Athenex.Whimclean.Api.AspNetCore

Framework-neutral ASP.NET Core API conventions: a consistent error contract, exception handling, status-code responses, and `Result` mapping. Works with FastEndpoints, Minimal APIs, or any other ASP.NET Core endpoint style.

## Install

```shell
dotnet add package Athenex.Whimclean.Api.AspNetCore
```

Consumers using FastEndpoints usually install `Athenex.Whimclean.Api.FastEndpoints` instead, which builds on this package and registers everything below automatically.

## Register services and middleware

```csharp
using Athenex.Whimclean.Api.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiErrorHandling();

var app = builder.Build();

app.UseApiErrorHandling();

app.MapGet("api/v1/orders", () => Results.Ok());

app.Run();
```

`AddApiErrorHandling` registers the exception handler pipeline and the default boundary-exception mappings. `UseApiErrorHandling` adds exception handling plus consistent 401/403/404 JSON responses for endpoints that end without a body.

## Error responses

Every failure uses the same shape:

```json
{
  "code": "Resource.NotFound",
  "description": "The requested resource was not found.",
  "errors": null
}
```

Default exception mappings:

- `CurrentUserRequiredException` → 401 `Authentication.CurrentUserRequired`
- `PersistenceConcurrencyException` → 409 `Persistence.ConcurrentUpdate`
- anything unmapped → 500 `Server.UnexpectedError` with details hidden

Add mappings for application exceptions by registering additional `IApiExceptionMapper` implementations.

## Current user from claims

`AddClaimsCurrentUser<TUserId>` registers a claims-based `ICurrentUser<TUserId>` so the auditing write boundary works with any authentication scheme that populates `HttpContext.User` — including JWT bearer tokens from an external identity provider such as Keycloak, Microsoft Entra ID, or Auth0. Whimclean does not store users or credentials; authenticate with your provider and the audit pipeline attributes writes from the token's claims.

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
builder.Services.AddClaimsCurrentUser<Guid>(options =>
    options.ClaimType = "sub");
```

Registration uses `TryAddScoped`, so an application-supplied `ICurrentUser<TUserId>` (for example, a background-job system user) wins over the claims implementation.

## Map results to HTTP

```csharp
using Athenex.Whimclean.Api.AspNetCore.Errors;

app.MapGet("api/v1/orders/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetOrderQuery(id));
    return result.ToHttpResult(order => Results.Ok(order));
});
```

`ErrorType` maps to status codes: `Validation` → 400, `Unauthorized` → 401, `Forbidden` → 403, `NotFound` → 404, `Conflict` → 409, `Failure` → 500.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
