# Athenex.Whimclean.Api.AspNetCore

[ภาษาไทย](README-TH.md)

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

`AddApiErrorHandling` registers the exception handler pipeline, ProblemDetails support, and the default boundary-exception mappings. `UseApiErrorHandling` adds exception handling plus consistent JSON bodies for 401 (`Authentication.Unauthorized`), 403 (`Authorization.Forbidden`) and 404 (`Resource.NotFound`) responses that end without a body.

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

Add mappings for application exceptions by registering additional `IApiExceptionMapper` implementations. Mappers are probed in registration order and the first match wins.

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

## Current user from a local table

When the application keeps its own user rows and the token only carries the identity provider's subject, resolve the local id per request:

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

`AddLocalUserResolution` registers the scoped `RequestCurrentUser<TUserId>` as `ICurrentUser<TUserId>` (via `TryAddScoped`, so an existing registration wins) and reads the subject from the claim named by `ClaimsCurrentUserOptions.ClaimType`. A default (empty) identifier means no local user; the current user stays unset and the write boundary rejects writes. The same scoped `RequestCurrentUser<TUserId>` implements `ICurrentUserInitializer<TUserId>`, so a startup bootstrap can attribute its writes after resolving its own user.

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

- `MapHealthEndpoints` maps `/health/live`, which answers as soon as the process serves requests, and `/health/ready`, which runs the checks tagged `HealthCheckTags.Ready`; both are anonymous and exempt from rate limiting. Both paths can be overridden through the `livePath` and `readyPath` parameters.
- `AddRequestRateLimiting` adds a global fixed window per caller, keyed by the `sub` claim (`user:{sub}`) or the remote address (`ip:{address}`), from the `RateLimiting` section (`PermitLimit`, default 300; `WindowSeconds`, default 60). Rejections answer 429. The options are checked with `RequestRateLimitOptions.Validate()` at registration: a permit limit or window that is not positive throws `InvalidOperationException` naming the setting. An overload takes a `RequestRateLimitOptions` instance instead of configuration.

## Validated options

`AddValidatedOptions` binds an options class to a section and validates it when the host starts, so a misconfigured deployment fails at boot with a message naming the setting:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Configuration;

builder.Services.AddValidatedOptions<EmailOptions>(
    builder.Configuration,
    "Email",
    options => string.IsNullOrWhiteSpace(options.Host) ? "Email:Host is required." : null);
```

The validation delegate returns a failure message, or `null` when the options are valid. `ConfigurationPlaceholder.IsUnset` recognizes the `<set-via-user-secrets>` marker (`ConfigurationPlaceholder.Value`) committed in `appsettings.json` for values that must come from user secrets or the environment; it also treats a missing or blank value as unset.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
