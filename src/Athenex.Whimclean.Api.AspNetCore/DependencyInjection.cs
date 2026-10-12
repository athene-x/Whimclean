using Athenex.Whimclean.Api.AspNetCore.Context;
using Athenex.Whimclean.Api.AspNetCore.Errors;
using Athenex.Whimclean.Application.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Athenex.Whimclean.Api.AspNetCore;

/// <summary>
/// Service registrations for the framework-neutral API error-handling
/// and current-user building blocks.
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

    /// <summary>
    /// Registers <see cref="ClaimsCurrentUser{TUserId}" /> as the
    /// <see cref="ICurrentUser{TUserId}" /> (via <c>TryAddScoped</c>, so
    /// an existing registration wins), resolving the user from a claim on
    /// the authenticated request principal (default
    /// <c>ClaimTypes.NameIdentifier</c>). Suits any authentication scheme
    /// populating the principal, including JWT bearer tokens from an
    /// external identity provider.
    /// </summary>
    /// <typeparam name="TUserId">
    /// Type of the user identifier; must be parsable from the claim
    /// value.
    /// </typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">
    /// Optional customization of
    /// <see cref="ClaimsCurrentUserOptions" />, e.g. to read the raw
    /// <c>sub</c> claim. Defaults to <see langword="null" />.
    /// </param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddClaimsCurrentUser<TUserId>(
        this IServiceCollection services,
        Action<ClaimsCurrentUserOptions>? configure = null)
        where TUserId : notnull, IParsable<TUserId>
    {
        services.AddHttpContextAccessor();
        services.AddOptions<ClaimsCurrentUserOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddScoped<
            ICurrentUser<TUserId>,
            ClaimsCurrentUser<TUserId>>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="RequestCurrentUser{TUserId}" /> as the scoped
    /// <see cref="ICurrentUser{TUserId}" /> (via <c>TryAddScoped</c>, so
    /// an existing registration wins) and as
    /// <see cref="ICurrentUserInitializer{TUserId}" />, for applications
    /// whose user identifier lives in a local table keyed by the token's
    /// subject. The application registers an
    /// <see cref="ILocalUserLookup{TUserId}" /> and adds
    /// <see cref="ApplicationBuilderExtensions.UseLocalUserResolution{TUserId}" />
    /// after authentication.
    /// </summary>
    /// <typeparam name="TUserId">Type of the local user identifier.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">
    /// Optional customization of
    /// <see cref="ClaimsCurrentUserOptions" />, which names the subject
    /// claim to look up. Defaults to <see langword="null" />.
    /// </param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddLocalUserResolution<TUserId>(
        this IServiceCollection services,
        Action<ClaimsCurrentUserOptions>? configure = null)
        where TUserId : notnull
    {
        services.AddOptions<ClaimsCurrentUserOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddScoped<RequestCurrentUser<TUserId>>();
        services.TryAddScoped<ICurrentUser<TUserId>>(provider =>
            provider.GetRequiredService<RequestCurrentUser<TUserId>>());
        services.TryAddScoped<ICurrentUserInitializer<TUserId>>(provider =>
            provider.GetRequiredService<RequestCurrentUser<TUserId>>());

        return services;
    }
}
