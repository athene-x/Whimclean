namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Options for <see cref="SlugTenantResolutionMiddleware{TTenantId}"/>,
/// controlling which request header carries the requested tenant's slug.
/// </summary>
public sealed class SlugTenantResolutionOptions
{
    /// <summary>
    /// Name of the request header carrying the tenant slug. Defaults to
    /// <c>X-Tenant-Slug</c>.
    /// </summary>
    public string HeaderName { get; set; } = "X-Tenant-Slug";
}
