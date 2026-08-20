using System.Diagnostics.CodeAnalysis;

namespace Athenex.Whimclean.Application.Context;

public interface ICurrentTenant<TTenantId>
    where TTenantId : notnull
{
    bool TryGetTenantId(
        [MaybeNullWhen(false)] out TTenantId tenantId);
}
