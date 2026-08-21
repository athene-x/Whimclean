using Athenex.Whimclean.Api.FastEndpoints.Errors;
using Athenex.Whimclean.Multitenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Athenex.Whimclean.Api.FastEndpoints.Multitenancy;

public static class DependencyInjection
{
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
