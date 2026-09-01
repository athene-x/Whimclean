using Athenex.Whimclean.Api.AspNetCore.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.AspNetCore;

/// <summary>
/// Pipeline configuration for the framework-neutral API error-handling
/// building block.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds exception handling (backed by <see cref="ApiExceptionHandler"/>)
    /// and status-code pages that give 401, 403, and 404 responses that
    /// ended without a body a consistent <see cref="ApiErrorResponse"/>
    /// JSON body. Requires the services added by
    /// <see cref="DependencyInjection.AddApiErrorHandling"/>.
    /// </summary>
    /// <param name="app">The application pipeline to configure.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IApplicationBuilder UseApiErrorHandling(
        this IApplicationBuilder app)
    {
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

        return app;
    }
}
