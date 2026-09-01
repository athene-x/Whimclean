namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Options for
/// <see cref="HeaderRequestedTenantResolver{TTenantId}"/>, controlling
/// which request header carries the requested tenant identifier.
/// </summary>
public sealed class HeaderTenantResolutionOptions
{
    /// <summary>
    /// Name of the request header carrying the requested tenant
    /// identifier. Defaults to <c>X-Tenant-Id</c>.
    /// </summary>
    public string HeaderName { get; set; } = "X-Tenant-Id";
}
