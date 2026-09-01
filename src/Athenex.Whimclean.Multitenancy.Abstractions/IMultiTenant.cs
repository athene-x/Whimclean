namespace Athenex.Whimclean.Multitenancy;

/// <summary>
/// Identifies a domain object that is owned by a tenant.
/// </summary>
public interface IMultiTenant<out TTenantId>
    where TTenantId : notnull
{
    /// <summary>
    /// The tenant this record belongs to. Enforced by the EF
    /// infrastructure package: a global query filter scopes reads to the
    /// current tenant, and write validation rejects cross-tenant changes.
    /// </summary>
    TTenantId TenantId { get; }
}
