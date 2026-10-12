using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Middleware that records the requested tenant from a slug header
/// (default <c>X-Tenant-Slug</c>) through
/// <see cref="ITenantSlugLookup{TTenantId}"/>, for deployments that
/// serve each tenant from a path or hostname and whose web tier forwards
/// the slug. It performs no authorization, and it does nothing when a
/// requested tenant was already recorded, so an explicit identifier from
/// <see cref="TenantResolutionMiddleware{TTenantId}"/> wins. Exactly one
/// header value is required; a repeated header resolves nothing.
/// </summary>
/// <typeparam name="TTenantId">Type of the tenant identifier.</typeparam>
/// <param name="next">The next middleware in the pipeline.</param>
public sealed class SlugTenantResolutionMiddleware<TTenantId>(RequestDelegate next)
    where TTenantId : notnull
{
    /// <summary>
    /// Looks the slug up and records the tenant it names, then always
    /// continues the pipeline.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext httpContext,
        IOptions<SlugTenantResolutionOptions> options,
        ITenantSlugLookup<TTenantId> lookup,
        TenantContext<TTenantId> tenantContext)
    {
        if (!tenantContext.TryGetRequestedTenantId(out _) &&
            TryReadSlug(httpContext.Request, options.Value.HeaderName, out var slug))
        {
            var tenantId = await lookup.FindBySlugAsync(slug, httpContext.RequestAborted);

            if (tenantId is not null &&
                !EqualityComparer<TTenantId>.Default.Equals(tenantId, default))
            {
                tenantContext.SetRequestedTenant(tenantId);
            }
        }

        await next(httpContext);
    }

    private static bool TryReadSlug(HttpRequest request, string headerName, out string slug)
    {
        if (TenantHeader.TryGetSingleValue(request, headerName, out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            slug = value.Trim().ToLowerInvariant();
            return true;
        }

        slug = string.Empty;
        return false;
    }
}
