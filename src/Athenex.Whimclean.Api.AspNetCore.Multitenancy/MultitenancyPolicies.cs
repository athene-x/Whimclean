namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Well-known authorization policy names registered by the multitenancy
/// building block.
/// </summary>
public static class MultitenancyPolicies
{
    /// <summary>
    /// Default name of the policy that requires an authenticated user who
    /// is a validated member of the requested tenant (see
    /// <see cref="TenantMemberRequirement"/>).
    /// </summary>
    public const string TenantMember = "TenantMember";
}
