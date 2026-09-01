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
    /// <c>AddApiErrorHandling</c>, and an OpenAPI document. Pair with
    /// <see cref="ApplicationBuilderExtensions.UseFastEndpointsApi"/>.
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
        Action<DocumentOptions>? configureOpenApi = null)
    {
        services.AddFastEndpoints();
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
