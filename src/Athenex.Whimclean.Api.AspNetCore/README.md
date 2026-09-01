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
