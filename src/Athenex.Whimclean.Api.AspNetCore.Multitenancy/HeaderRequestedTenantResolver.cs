using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Resolves the requested tenant from a request header whose name is
/// configured through <see cref="HeaderTenantResolutionOptions"/>
/// (default <c>X-Tenant-Id</c>). Exactly one header value is required: a
/// missing header resolves nothing, and a repeated one is treated as
/// ambiguous rather than picking a value.
/// </summary>
/// <typeparam name="TTenantId">
/// Type of the tenant identifier; parsed from the header value with the
/// invariant culture.
/// </typeparam>
/// <param name="options">
/// Supplies the <see cref="HeaderTenantResolutionOptions.HeaderName"/> to
/// read.
/// </param>
public sealed class HeaderRequestedTenantResolver<TTenantId>(
    IOptions<HeaderTenantResolutionOptions> options)
    : IRequestedTenantResolver<TTenantId>
    where TTenantId : notnull, IParsable<TTenantId>
{
    /// <summary>
    /// Reads the configured tenant header and parses its single value
    /// using the invariant culture.
    /// </summary>
    /// <param name="httpContext">The request to inspect.</param>
    /// <param name="tenantId">
    /// The parsed tenant identifier, or the default value on failure.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the header appeared exactly once and its
    /// value parsed as a <typeparamref name="TTenantId"/>; otherwise
    /// <see langword="false"/>.
    /// </returns>
    public bool TryResolve(
        HttpContext httpContext,
        out TTenantId tenantId)
    {
        var values = httpContext.Request.Headers[options.Value.HeaderName];

        // A repeated tenant header is ambiguous; resolve no tenant rather
        // than silently picking one of the conflicting values.
        if (values.Count != 1)
        {
            tenantId = default!;
            return false;
        }

        return TTenantId.TryParse(
            values[0],
            CultureInfo.InvariantCulture,
            out tenantId!);
    }
}
