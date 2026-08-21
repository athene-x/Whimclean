using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Api.FastEndpoints.Multitenancy;

public sealed class HeaderRequestedTenantResolver<TTenantId>(
    IOptions<HeaderTenantResolutionOptions> options)
    : IRequestedTenantResolver<TTenantId>
    where TTenantId : notnull, IParsable<TTenantId>
{
    public bool TryResolve(
        HttpContext httpContext,
        out TTenantId tenantId)
    {
        var value = httpContext.Request.Headers[options.Value.HeaderName]
            .FirstOrDefault();

        return TTenantId.TryParse(
            value,
            CultureInfo.InvariantCulture,
            out tenantId!);
    }
}
