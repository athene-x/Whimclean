# Athenex.Whimclean.Api.FastEndpoints

FastEndpoints conventions for OpenAPI, validation errors, status-code errors, application results, and boundary exceptions.

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

`UseFastEndpointsApi` applies the default `api/v1` route prefix, exception handling, consistent empty 401/403/404 responses, FastEndpoints validation mapping, and endpoint discovery.

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
    document.DocumentSettings = settings =>
        settings.Title = "Orders API";
});
```

## Map application errors

Convert a failed Whimclean `Error` into the standard HTTP response:

```csharp
using Athenex.Whimclean.Api.FastEndpoints.Errors;

var result = await handler.HandleAsync(request, cancellationToken);

if (result.IsFailure)
{
    await SendResultAsync(result.Error.ToHttpResult());
    return;
}
```

Mappings are validation → 400, not found → 404, conflict → 409, and general failure → 500.

Responses use this shape:

```json
{
  "code": "Order.NotFound",
  "message": "The order was not found.",
  "details": []
}
```

FastEndpoints validation failures populate `details` with code, message, and camel-cased field.

## Built-in exception mappings

- `CurrentUserRequiredException` → 401 `Authentication.CurrentUserRequired`
- `PersistenceConcurrencyException` → 409 `Persistence.ConcurrentUpdate`
- unexpected exceptions → 500 `Server.UnexpectedError`

Unexpected details are logged but never returned to the client.

## Add a product-specific exception mapping

```csharp
using Athenex.Whimclean.Api.FastEndpoints.Errors;

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

The optional ASP.NET Core multitenancy package contributes tenant mappings through the same extension point.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
