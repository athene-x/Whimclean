using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.FastEndpoints.Multitenancy;

public sealed class TenantResolutionMiddleware<TTenantId>(
    RequestDelegate next)
    where TTenantId : notnull
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        IRequestedTenantResolver<TTenantId> resolver,
        TenantContext<TTenantId> tenantContext)
    {
        if (resolver.TryResolve(httpContext, out var tenantId))
        {
            tenantContext.SetRequestedTenant(tenantId);
        }

        await next(httpContext);
    }
}
