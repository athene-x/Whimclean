using Athenex.Whimclean.Api.AspNetCore.Context;
using Athenex.Whimclean.Api.AspNetCore.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.AspNetCore;

/// <summary>
/// Pipeline configuration for the Minimal API host and the API
/// error-handling building block.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Route pattern of the OpenAPI document mapped by
    /// <see cref="UseMinimalApi"/> by default.
    /// </summary>
    public const string DefaultOpenApiRoutePattern = "/openapi/{documentName}.json";

    /// <summary>
    /// Configures the request pipeline for a Minimal API: the shared error
    /// handling from <see cref="UseApiErrorHandling"/> plus the OpenAPI
    /// document endpoint. Requires the services added by
    /// <see cref="DependencyInjection.AddMinimalApi"/>. Map endpoints
    /// afterwards through
    /// <see cref="EndpointRouteBuilderExtensions.MapApiGroup"/>.
    /// </summary>
    /// <param name="app">The application to configure.</param>
    /// <param name="openApiRoutePattern">
    /// Route of the OpenAPI document; defaults to
    /// <see cref="DefaultOpenApiRoutePattern"/>. Pass
    /// <see langword="null"/> to leave the document unmapped, for example
    /// to map it only in development.
    /// </param>
    /// <returns>The same application, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="app"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="openApiRoutePattern"/> is empty or whitespace.
    /// </exception>
    public static WebApplication UseMinimalApi(
        this WebApplication app,
        string? openApiRoutePattern = DefaultOpenApiRoutePattern)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseApiErrorHandling();

        if (openApiRoutePattern is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(openApiRoutePattern);
            app.MapOpenApi(openApiRoutePattern);
        }

        return app;
    }

    /// <summary>
    /// Adds exception handling (backed by <see cref="ApiExceptionHandler"/>)
    /// and status-code pages that give 400, 401, 403, and 404 responses
    /// that ended without a body a consistent <see cref="ApiErrorResponse"/>
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
                StatusCodes.Status400BadRequest => new ApiErrorResponse(
                    "Request.Invalid",
                    "The request is invalid."),
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

    /// <summary>
    /// Adds <see cref="LocalUserResolutionMiddleware{TUserId}" />, which
    /// maps the authenticated subject to the local user registered
    /// through <see cref="DependencyInjection.AddLocalUserResolution{TUserId}" />.
    /// Place after <c>UseAuthentication</c> and before anything that reads
    /// the current user, such as tenant resolution and authorization.
    /// </summary>
    /// <typeparam name="TUserId">Type of the local user identifier.</typeparam>
    /// <param name="app">The application pipeline to configure.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IApplicationBuilder UseLocalUserResolution<TUserId>(
        this IApplicationBuilder app)
        where TUserId : notnull =>
        app.UseMiddleware<LocalUserResolutionMiddleware<TUserId>>();
}
