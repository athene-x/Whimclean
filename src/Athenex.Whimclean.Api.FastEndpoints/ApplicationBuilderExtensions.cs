using System.Text.Json;
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Api.AspNetCore.Errors;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;

namespace Athenex.Whimclean.Api.FastEndpoints;

/// <summary>
/// Pipeline configuration for the FastEndpoints API adapter.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the request pipeline for a FastEndpoints-based API: the
    /// shared error handling from <c>UseApiErrorHandling</c>, plus
    /// FastEndpoints with the given route prefix and with validation
    /// failures rendered as the shared <see cref="ApiErrorResponse"/>
    /// (code <c>"Validation.Failed"</c>, camelCase field names). Requires
    /// the services added by
    /// <see cref="DependencyInjection.AddFastEndpointsApi"/>.
    /// </summary>
    /// <param name="app">The application pipeline to configure.</param>
    /// <param name="routePrefix">
    /// Prefix prepended to every endpoint route. Defaults to
    /// <c>"api/v1"</c>.
    /// </param>
    /// <param name="configure">
    /// Optional hook to adjust the FastEndpoints <see cref="Config"/>
    /// after the adapter's settings are applied. Defaults to
    /// <see langword="null"/>.
    /// </param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="routePrefix"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="routePrefix"/> is empty or whitespace.
    /// </exception>
    public static IApplicationBuilder UseFastEndpointsApi(
        this IApplicationBuilder app,
        string routePrefix = "api/v1",
        Action<Config>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);

        app.UseApiErrorHandling();

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
