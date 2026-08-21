using System.Diagnostics.CodeAnalysis;

namespace Athenex.Whimclean.Multitenancy;

public interface ICurrentTenant<TTenantId>
    where TTenantId : notnull
{
    bool TryGetTenantId(
        [MaybeNullWhen(false)] out TTenantId tenantId);
}
