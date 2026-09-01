using System.Diagnostics.CodeAnalysis;

namespace Athenex.Whimclean.Multitenancy;

/// <summary>
/// The tenant the caller CLAIMS to act for, extracted from the transport
/// (e.g. a request header). The claim is UNVERIFIED — nothing here proves
/// the caller may access that tenant — so it must only feed
/// <see cref="ITenantAccessValidator{TUserId, TTenantId}" />; once
/// validation succeeds the tenant becomes available through
/// <see cref="ICurrentTenant{TTenantId}" />, which is the contract
/// trusted for filtering and writes.
/// </summary>
/// <typeparam name="TTenantId">The tenant identifier type.</typeparam>
public interface IRequestedTenant<TTenantId>
    where TTenantId : notnull
{
    /// <summary>
    /// Retrieves the claimed tenant, if the transport carried one that
    /// could be parsed. Returns <see langword="false" /> — leaving
    /// <paramref name="tenantId" /> default — when no usable claim is
    /// present. A <see langword="true" /> result is still unverified.
    /// </summary>
    /// <param name="tenantId">
    /// The claimed tenant id; meaningful only when the method returns
    /// <see langword="true" />.
    /// </param>
    bool TryGetTenantId(
        [MaybeNullWhen(false)] out TTenantId tenantId);
}
