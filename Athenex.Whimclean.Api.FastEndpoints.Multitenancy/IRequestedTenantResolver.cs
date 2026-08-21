using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.FastEndpoints.Multitenancy;

public interface IRequestedTenantResolver<TTenantId>
    where TTenantId : notnull
{
    bool TryResolve(
        HttpContext httpContext,
        [MaybeNullWhen(false)] out TTenantId tenantId);
}
