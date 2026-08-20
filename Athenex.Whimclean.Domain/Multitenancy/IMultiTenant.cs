namespace Athenex.Whimclean.Domain.Multitenancy;

/// <summary>
/// Identifies a domain object that is owned by a tenant.
/// </summary>
public interface IMultiTenant<out TTenantId>
    where TTenantId : notnull
{
    TTenantId TenantId { get; }
}
