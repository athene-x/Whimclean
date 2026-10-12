using System.Reflection;
using Athenex.Whimclean.Api.AspNetCore.Context;
using Athenex.Whimclean.Api.AspNetCore.Errors;
using Athenex.Whimclean.Application.Context;
using FluentValidation;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Athenex.Whimclean.Api.AspNetCore;

/// <summary>
/// Service registrations for the Minimal API host, request validation,
/// error handling, and current-user building blocks.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers everything a Minimal API needs in one call: the shared
    /// error handling from <see cref="AddApiErrorHandling"/> and an
    /// OpenAPI document. Pair with
    /// <see cref="ApplicationBuilderExtensions.UseMinimalApi"/>, map
    /// endpoints through
    /// <see cref="EndpointRouteBuilderExtensions.MapApiGroup"/>, and
    /// register validators with
    /// <see cref="AddApiValidatorsFromAssembly"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureOpenApi">
    /// Optional customization of the OpenAPI document; when
    /// <see langword="null"/> (the default) the document uses the
    /// ASP.NET Core defaults.
    /// </param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddMinimalApi(
        this IServiceCollection services,
        Action<OpenApiOptions>? configureOpenApi = null)
    {
        services.AddApiErrorHandling();

        if (configureOpenApi is null)
        {
            services.AddOpenApi();
        }
        else
        {
            services.AddOpenApi(configureOpenApi);
        }

        return services;
    }

    /// <summary>
    /// Registers every concrete <c>IValidator&lt;T&gt;</c> implementation
    /// in <paramref name="assembly"/> under its validator interface (via
    /// <c>TryAdd</c>, so an explicit registration wins), for the request
    /// validation applied by
    /// <see cref="EndpointRouteBuilderExtensions.WithRequestValidation{TBuilder}"/>.
    /// Abstract and open generic validators are skipped.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="lifetime">
    /// Lifetime of the validators; defaults to
    /// <see cref="ServiceLifetime.Scoped"/> so validators can depend on
    /// scoped services.
    /// </param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="assembly"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddApiValidatorsFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var type in GetLoadableTypes(assembly))
        {
            if (type is not { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            {
                continue;
            }

            foreach (var validatorInterface in type.GetInterfaces())
            {
                if (validatorInterface.IsGenericType &&
                    validatorInterface.GetGenericTypeDefinition() == typeof(IValidator<>))
                {
                    services.TryAdd(new ServiceDescriptor(validatorInterface, type, lifetime));
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Registers the validators of the assembly containing
    /// <typeparamref name="TMarker"/>; see
    /// <see cref="AddApiValidatorsFromAssembly"/>.
    /// </summary>
    /// <typeparam name="TMarker">Any type in the assembly to scan.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="lifetime">
    /// Lifetime of the validators; defaults to
    /// <see cref="ServiceLifetime.Scoped"/>.
    /// </param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddApiValidatorsFromAssemblyContaining<TMarker>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped) =>
        services.AddApiValidatorsFromAssembly(typeof(TMarker).Assembly, lifetime);

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

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null)!;
        }
    }
}
