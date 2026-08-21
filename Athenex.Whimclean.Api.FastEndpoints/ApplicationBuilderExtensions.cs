using System.Text.Json;
using Athenex.Whimclean.Api.FastEndpoints.Errors;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.FastEndpoints;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseFastEndpointsApi(
        this IApplicationBuilder app,
        string routePrefix = "api/v1",
        Action<Config>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);

        app.UseExceptionHandler();
        app.UseStatusCodePages(async statusCodeContext =>
        {
            var response = statusCodeContext.HttpContext.Response;
            var error = response.StatusCode switch
            {
                StatusCodes.Status401Unauthorized => new ApiErrorResponse(
                    "Authentication.Unauthorized",
                    "Authentication is required to access this resource."),
                StatusCodes.Status403Forbidden => new ApiErrorResponse(
                    "Authorization.Forbidden",
                    "You are not allowed to access this resource."),
                StatusCodes.Status404NotFound => new ApiErrorResponse(
                    "Resource.NotFound",
                    "The requested resource was not found."),
                _ => null
            };

            if (error is not null)
            {
                await response.WriteAsJsonAsync(
                    error,
                    statusCodeContext.HttpContext.RequestAborted);
            }
        });

        app.UseFastEndpoints(options =>
        {
            options.Endpoints.RoutePrefix = routePrefix;
            options.Errors.ProducesMetadataType = typeof(ApiErrorResponse);
            options.Errors.ResponseBuilder = (failures, _, _) =>
                new ApiErrorResponse(
                    "Validation.Failed",
                    "One or more validation errors occurred.",
                    failures
                        .Select(failure => new ApiErrorDetail(
                            failure.ErrorCode,
                            failure.ErrorMessage,
                            JsonNamingPolicy.CamelCase.ConvertName(
                                failure.PropertyName)))
                        .ToArray());

            configure?.Invoke(options);
        });

        return app;
    }
}
