using Athenex.Whimclean.Api.AspNetCore.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Athenex.Whimclean.Api.AspNetCore;

/// <summary>
/// Endpoint mapping conventions for Minimal APIs built on the Whimclean
/// API package.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>Route prefix used by <see cref="MapApiGroup"/> by default.</summary>
    public const string DefaultRoutePrefix = "api/v1";

    /// <summary>
    /// Creates the route group every API endpoint maps into: the given
    /// prefix (default <c>api/v1</c>) with request validation applied, so
    /// a handler argument whose type has a registered FluentValidation
    /// validator is validated before the handler runs and a failure
    /// answers 400 <c>"Validation.Failed"</c>. Register validators with
    /// <see cref="DependencyInjection.AddApiValidatorsFromAssembly"/>.
    /// </summary>
    /// <param name="endpoints">The route builder to map onto.</param>
    /// <param name="routePrefix">
    /// Prefix prepended to every endpoint route in the group. Defaults to
    /// <see cref="DefaultRoutePrefix"/>.
    /// </param>
    /// <returns>The group, for mapping endpoints and adding conventions.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="endpoints"/> or <paramref name="routePrefix"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="routePrefix"/> is empty or whitespace.
    /// </exception>
    public static RouteGroupBuilder MapApiGroup(
        this IEndpointRouteBuilder endpoints,
        string routePrefix = DefaultRoutePrefix)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);

        return endpoints.MapGroup(routePrefix).WithRequestValidation();
    }

    /// <summary>
    /// Validates handler arguments with the <c>IValidator&lt;T&gt;</c>
    /// registered for their type and short-circuits with the shared 400
    /// <c>"Validation.Failed"</c> response (see
    /// <see cref="Errors.ValidationHttpMapper"/>). Each endpoint is
    /// inspected once at startup; endpoints without a validated argument
    /// run unchanged. Applied by <see cref="MapApiGroup"/>; call it
    /// directly on endpoints or groups mapped elsewhere.
    /// </summary>
    /// <typeparam name="TBuilder">The convention builder type.</typeparam>
    /// <param name="builder">The endpoint or group to validate.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    public static TBuilder WithRequestValidation<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddEndpointFilterFactory(RequestValidationFilter.Create);

        return builder;
    }
}
