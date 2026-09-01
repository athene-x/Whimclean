using Athenex.Whimclean.Api.AspNetCore.Errors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Athenex.Whimclean.Api.AspNetCore;

/// <summary>
/// Service registrations for the framework-neutral API error-handling
/// building block.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers consistent API error handling: the
    /// <see cref="IApiExceptionMapper"/> chain (seeded with
    /// <see cref="DefaultApiExceptionMapper"/> via
    /// <c>TryAddEnumerable</c>, so other packages can add mappers without
    /// overriding it), the <see cref="ApiExceptionHandler"/>, and
    /// ProblemDetails support. Pair with
    /// <see cref="ApplicationBuilderExtensions.UseApiErrorHandling"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddApiErrorHandling(
        this IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IApiExceptionMapper,
                DefaultApiExceptionMapper>());
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
