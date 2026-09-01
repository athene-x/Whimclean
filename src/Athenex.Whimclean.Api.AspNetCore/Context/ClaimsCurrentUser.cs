using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Athenex.Whimclean.Application.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Api.AspNetCore.Context;

/// <summary>
/// Resolves the current user from the authenticated request principal's
/// claims, so any authentication scheme that populates
/// <see cref="HttpContext.User" /> — cookie, JWT bearer from an external
/// identity provider, or otherwise — can attribute writes without a
/// custom <see cref="ICurrentUser{TUserId}" /> implementation. The claim
/// to read is configured through
/// <see cref="ClaimsCurrentUserOptions" /> (default
/// <c>ClaimTypes.NameIdentifier</c>).
/// </summary>
/// <typeparam name="TUserId">
/// Type of the user identifier; parsed from the claim value with the
/// invariant culture.
/// </typeparam>
/// <param name="httpContextAccessor">
/// Supplies the request whose principal is inspected.
/// </param>
/// <param name="options">
/// Supplies the <see cref="ClaimsCurrentUserOptions.ClaimType" /> to
/// read.
/// </param>
public sealed class ClaimsCurrentUser<TUserId>(
    IHttpContextAccessor httpContextAccessor,
    IOptions<ClaimsCurrentUserOptions> options)
    : ICurrentUser<TUserId>
    where TUserId : notnull, IParsable<TUserId>
{
    /// <summary>
    /// Reads the configured claim from the authenticated principal and
    /// parses its value using the invariant culture.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if a request with an authenticated
    /// principal is active, the claim is present, and its value parsed as
    /// a <typeparamref name="TUserId" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool TryGetUserId([MaybeNullWhen(false)] out TUserId userId)
    {
        var principal = httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            userId = default;
            return false;
        }

        var claimValue = principal.FindFirst(options.Value.ClaimType)?.Value;

        if (string.IsNullOrEmpty(claimValue))
        {
            userId = default;
            return false;
        }

        return TUserId.TryParse(
            claimValue,
            CultureInfo.InvariantCulture,
            out userId);
    }
}
