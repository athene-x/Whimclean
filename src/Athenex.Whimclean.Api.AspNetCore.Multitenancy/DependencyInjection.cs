using Athenex.Whimclean.Api.AspNetCore.Errors;
using Athenex.Whimclean.Multitenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Service registrations for the ASP.NET Core multitenancy building
/// block.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the multitenancy request pipeline services: the scoped
    /// <see cref="TenantContext{TTenantId}"/> (exposed as both
    /// <see cref="IRequestedTenant{TTenantId}"/> and
    /// <see cref="ICurrentTenant{TTenantId}"/>), the
    /// <see cref="TenantMemberAuthorizationHandler{TUserId, TTenantId}"/>,
    /// the <see cref="TenantApiExceptionMapper"/> (added via
    /// <c>TryAddEnumerable</c> alongside other mappers), and an
    /// authorization policy requiring an authenticated tenant member.
    /// Complete the setup with
    /// <see cref="ApplicationBuilderExtensions.UseTenantResolution{TTenantId}"/>
    /// and a resolver such as
    /// <see cref="AddHeaderTenantResolution{TTenantId}"/>.
    /// </summary>
    /// <typeparam name="TUserId">Type of the user identifier.</typeparam>
    /// <typeparam name="TTenantId">Type of the tenant identifier.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="policyName">
    /// Name under which the tenant-member policy is registered. Defaults
    /// to <see cref="MultitenancyPolicies.TenantMember"/>.
    /// </param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="policyName"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="policyName"/> is empty or whitespace.
    /// </exception>
    public static IServiceCollection AddAspNetCoreMultitenancy<
        TUserId,
        TTenantId>(
        this IServiceCollection services,
        string policyName = MultitenancyPolicies.TenantMember)
        where TUserId : notnull
        where TTenantId : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        services.AddScoped<TenantContext<TTenantId>>();
        services.AddScoped<IRequestedTenant<TTenantId>>(serviceProvider =>
            serviceProvider.GetRequiredService<TenantContext<TTenantId>>());
        services.AddScoped<ICurrentTenant<TTenantId>>(serviceProvider =>
            serviceProvider.GetRequiredService<TenantContext<TTenantId>>());
        services.AddScoped<
            IAuthorizationHandler,
            TenantMemberAuthorizationHandler<TUserId, TTenantId>>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IApiExceptionMapper,
                TenantApiExceptionMapper>());

        services.AddAuthorizationBuilder()
            .AddPolicy(policyName, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new TenantMemberRequirement());
            });

        return services;
    }

    /// <summary>
    /// Registers <see cref="HeaderRequestedTenantResolver{TTenantId}"/>
    /// as the <see cref="IRequestedTenantResolver{TTenantId}"/> (via
    /// <c>TryAddScoped</c>, so an existing resolver registration wins),
    /// reading the tenant from a request header (default
    /// <c>X-Tenant-Id</c>).
    /// </summary>
    /// <typeparam name="TTenantId">
    /// Type of the tenant identifier; must be parsable from the header
    /// value.
    /// </typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">
    /// Optional customization of
    /// <see cref="HeaderTenantResolutionOptions"/>, e.g. to change the
    /// header name. Defaults to <see langword="null"/>.
    /// </param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddHeaderTenantResolution<TTenantId>(
        this IServiceCollection services,
        Action<HeaderTenantResolutionOptions>? configure = null)
        where TTenantId : notnull, IParsable<TTenantId>
    {
        services.AddOptions<HeaderTenantResolutionOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddScoped<
            IRequestedTenantResolver<TTenantId>,
            HeaderRequestedTenantResolver<TTenantId>>();

        return services;
    }
}
