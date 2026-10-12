using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Api.AspNetCore.Errors;
using Athenex.Whimclean.Api.FastEndpoints.Queries;
using FastEndpoints;
using FastEndpoints.OpenApi;
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
    /// failures rendered through the shared
    /// <see cref="ValidationHttpMapper"/> (code <c>"Validation.Failed"</c>,
    /// camelCase field names), and list query filters bound whole
    /// (<see cref="ListQueryBinding.KeepListQueryFiltersWhole"/>). Requires
    /// the services added by
    /// <see cref="DependencyInjection.AddFastEndpointsApi(Microsoft.Extensions.DependencyInjection.IServiceCollection, Action{DocumentOptions}?)"/>.
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
            options.Binding.KeepListQueryFiltersWhole();
            options.Errors.ProducesMetadataType = typeof(ApiErrorResponse);
            options.Errors.ResponseBuilder = (failures, _, _) =>
                failures.ToApiErrorResponse();

            configure?.Invoke(options);
        });

        return app;
    }
}
