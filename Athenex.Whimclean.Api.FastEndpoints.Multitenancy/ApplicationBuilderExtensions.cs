using Microsoft.AspNetCore.Builder;

namespace Athenex.Whimclean.Api.FastEndpoints.Multitenancy;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseTenantResolution<TTenantId>(
        this IApplicationBuilder app)
        where TTenantId : notnull =>
        app.UseMiddleware<TenantResolutionMiddleware<TTenantId>>();
}
