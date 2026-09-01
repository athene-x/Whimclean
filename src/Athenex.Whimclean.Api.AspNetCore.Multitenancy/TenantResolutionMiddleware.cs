using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Middleware that records the tenant a request claims to target, using
/// the registered <see cref="IRequestedTenantResolver{TTenantId}"/>. It
/// performs no authorization: the recorded tenant stays a mere claim
/// until access is validated and the current tenant is established.
/// </summary>
/// <typeparam name="TTenantId">Type of the tenant identifier.</typeparam>
/// <param name="next">The next middleware in the pipeline.</param>
public sealed class TenantResolutionMiddleware<TTenantId>(
    RequestDelegate next)
    where TTenantId : notnull
{
    /// <summary>
    /// Records the requested tenant on the scoped
    /// <see cref="TenantContext{TTenantId}"/> when the resolver finds
    /// one, then always continues the pipeline — an unresolved tenant is
    /// not an error at this stage.
    /// </summary>
    /// <param name="httpContext">The request being processed.</param>
    /// <param name="resolver">
    /// Extracts the requested tenant from the request.
    /// </param>
    /// <param name="tenantContext">
    /// The per-request holder that receives the requested tenant.
    /// </param>
    /// <returns>A task that completes when the pipeline has run.</returns>
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
