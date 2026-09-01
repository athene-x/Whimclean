using System.Diagnostics.CodeAnalysis;

namespace Athenex.Whimclean.Multitenancy;

/// <summary>
/// The tenant that has been ESTABLISHED for the current execution — i.e.
/// access validation has already succeeded for it. This is the security
/// boundary's trusted side: data filtering and write attribution key off
/// this contract, never off the unverified
/// <see cref="IRequestedTenant{TTenantId}" /> claim.
/// </summary>
/// <typeparam name="TTenantId">The tenant identifier type.</typeparam>
public interface ICurrentTenant<TTenantId>
    where TTenantId : notnull
{
    /// <summary>
    /// Retrieves the established tenant, if any. Returns
    /// <see langword="false" /> — leaving <paramref name="tenantId" />
    /// default — when the current execution is not tenant-scoped; it
    /// never returns an unvalidated tenant.
    /// </summary>
    /// <param name="tenantId">
    /// The established tenant id; meaningful only when the method
    /// returns <see langword="true" />.
    /// </param>
    bool TryGetTenantId(
        [MaybeNullWhen(false)] out TTenantId tenantId);
}
