namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore;

/// <summary>
/// Names of the global query filters applied by the library's DbContexts,
/// for targeted disabling via <c>IgnoreQueryFilters([...])</c>. Prefer the
/// dedicated helpers (e.g. <c>IncludeSoftDeleted()</c>) over bare
/// <c>IgnoreQueryFilters()</c>, which would also drop tenant isolation.
/// </summary>
public static class QueryFilterNames
{
    /// <summary>Hides records flagged by ISoftDelete from default queries.</summary>
    public const string SoftDelete = "SoftDeleteFilter";

    /// <summary>
    /// Restricts records to the current tenant. Applied when the
    /// EntityFrameworkCore.Multitenancy package's context is used.
    /// </summary>
    public const string Tenant = "TenantFilter";
}
