using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Extracts the tenant a request claims to target from the incoming HTTP
/// request. Resolution only records the claim — it performs no
/// authorization; access is validated separately before the tenant
/// becomes current.
/// </summary>
/// <typeparam name="TTenantId">Type of the tenant identifier.</typeparam>
public interface IRequestedTenantResolver<TTenantId>
    where TTenantId : notnull
{
    /// <summary>
    /// Attempts to extract the requested tenant identifier from
    /// <paramref name="httpContext"/>.
    /// </summary>
    /// <param name="httpContext">The request to inspect.</param>
    /// <param name="tenantId">
    /// The resolved tenant identifier, or the default value when the
    /// request carries none.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the request unambiguously identified a
    /// tenant; otherwise <see langword="false"/>.
    /// </returns>
    bool TryResolve(
        HttpContext httpContext,
        [MaybeNullWhen(false)] out TTenantId tenantId);
}
