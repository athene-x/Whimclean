using Athenex.Whimclean.Api.AspNetCore;
using FastEndpoints;
using FastEndpoints.OpenApi;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Api.FastEndpoints;

/// <summary>
/// Service registrations for the FastEndpoints API adapter.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers everything a FastEndpoints-based API needs in one call:
    /// FastEndpoints itself, the shared error handling from
    /// <c>AddApiErrorHandling</c>, and a FastEndpoints OpenAPI document.
    /// Pair with <see cref="ApplicationBuilderExtensions.UseFastEndpointsApi"/>.
    /// Endpoint discovery includes plain FluentValidation
    /// <c>AbstractValidator&lt;TRequest&gt;</c> descendants
    /// (<see cref="EndpointDiscoveryOptions.IncludeAbstractValidators"/>),
    /// so validators derived from the shared
    /// <c>ListQueryRequestValidator&lt;TRequest&gt;</c> are picked up as
    /// request validators without <c>AddApiValidatorsFromAssembly</c>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureOpenApi">
    /// Optional customization of the OpenAPI document; when
    /// <see langword="null"/> (the default) the document uses the
    /// FastEndpoints defaults.
    /// </param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFastEndpointsApi(
        this IServiceCollection services,
        Action<DocumentOptions>? configureOpenApi = null) =>
        services.AddFastEndpointsApi(configureOpenApi, configureDiscovery: null);

    /// <summary>
    /// Same as <see cref="AddFastEndpointsApi(IServiceCollection, Action{DocumentOptions}?)"/>,
    /// with a hook to adjust FastEndpoints endpoint discovery after the
    /// adapter's settings are applied, for example to limit the scanned
    /// assemblies.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureOpenApi">
    /// Optional customization of the OpenAPI document; <see langword="null"/>
    /// keeps the FastEndpoints defaults.
    /// </param>
    /// <param name="configureDiscovery">
    /// Optional customization of <see cref="EndpointDiscoveryOptions"/>;
    /// <see langword="null"/> keeps the adapter's settings.
    /// </param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFastEndpointsApi(
        this IServiceCollection services,
        Action<DocumentOptions>? configureOpenApi,
        Action<EndpointDiscoveryOptions>? configureDiscovery)
    {
        services.AddFastEndpoints(discovery =>
        {
            discovery.IncludeAbstractValidators = true;
            configureDiscovery?.Invoke(discovery);
        });
        services.AddApiErrorHandling();

        if (configureOpenApi is null)
        {
            services.OpenApiDocument();
        }
        else
        {
            services.OpenApiDocument(configureOpenApi);
        }

        return services;
    }
}
