using Athenex.Whimclean.Multitenancy;

namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Per-request scoped holder for the two distinct tenant notions of a
/// request: the requested tenant — an unverified claim taken from the
/// transport — and the current tenant, which exists only after tenant
/// access has been validated. Keeping the two behind separate interfaces
/// (<see cref="IRequestedTenant{TTenantId}"/> versus
/// <see cref="ICurrentTenant{TTenantId}"/>) is the security boundary:
/// consumers of the current tenant never see an unvalidated value.
/// </summary>
/// <typeparam name="TTenantId">Type of the tenant identifier.</typeparam>
public sealed class TenantContext<TTenantId> :
    IRequestedTenant<TTenantId>,
    ICurrentTenant<TTenantId>
    where TTenantId : notnull
{
    private TTenantId? requestedTenantId;
    private TTenantId? currentTenantId;

    private bool HasRequestedTenant { get; set; }
    private bool HasCurrentTenant { get; set; }

    /// <summary>
    /// Gets the tenant the request claims to target, as recorded by
    /// <see cref="TenantResolutionMiddleware{TTenantId}"/>. This value is
    /// unverified — it says nothing about whether the caller may access
    /// that tenant.
    /// </summary>
    /// <param name="tenantId">
    /// The requested tenant identifier when one was recorded; otherwise
    /// undefined.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a requested tenant was recorded for this
    /// request; otherwise <see langword="false"/>.
    /// </returns>
    public bool TryGetRequestedTenantId(out TTenantId tenantId)
    {
        tenantId = requestedTenantId!;
        return HasRequestedTenant;
    }

    bool IRequestedTenant<TTenantId>.TryGetTenantId(
        out TTenantId tenantId) =>
        TryGetRequestedTenantId(out tenantId);

    /// <summary>
    /// Gets the established current tenant. A value is only available
    /// after the caller's access to the requested tenant has been
    /// validated (see
    /// <see cref="TenantMemberAuthorizationHandler{TUserId, TTenantId}"/>),
    /// so it is always safe to act on.
    /// </summary>
    /// <param name="tenantId">
    /// The current tenant identifier when one has been established;
    /// otherwise undefined.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a current tenant has been established for
    /// this request; otherwise <see langword="false"/>.
    /// </returns>
    public bool TryGetTenantId(out TTenantId tenantId)
    {
        tenantId = currentTenantId!;
        return HasCurrentTenant;
    }

    internal void SetRequestedTenant(TTenantId tenantId)
    {
        requestedTenantId = tenantId;
        HasRequestedTenant = true;
    }

    internal void EstablishCurrentTenant(TTenantId tenantId)
    {
        currentTenantId = tenantId;
        HasCurrentTenant = true;
    }
}
