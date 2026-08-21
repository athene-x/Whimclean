using Athenex.Whimclean.Multitenancy;

namespace Athenex.Whimclean.Api.FastEndpoints.Multitenancy;

public sealed class TenantContext<TTenantId> :
    IRequestedTenant<TTenantId>,
    ICurrentTenant<TTenantId>
    where TTenantId : notnull
{
    private TTenantId? requestedTenantId;
    private TTenantId? currentTenantId;

    private bool HasRequestedTenant { get; set; }
    private bool HasCurrentTenant { get; set; }

    public bool TryGetRequestedTenantId(out TTenantId tenantId)
    {
        tenantId = requestedTenantId!;
        return HasRequestedTenant;
    }

    bool IRequestedTenant<TTenantId>.TryGetTenantId(
        out TTenantId tenantId) =>
        TryGetRequestedTenantId(out tenantId);

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
