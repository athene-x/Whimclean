namespace Athenex.Whimclean.Api.FastEndpoints.Multitenancy;

public sealed class HeaderTenantResolutionOptions
{
    public string HeaderName { get; set; } = "X-Tenant-Id";
}
