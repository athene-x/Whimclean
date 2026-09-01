using System.Security.Claims;

namespace Athenex.Whimclean.Api.AspNetCore.Context;

/// <summary>
/// Options for <see cref="ClaimsCurrentUser{TUserId}" />, controlling
/// which claim on the authenticated principal carries the user
/// identifier.
/// </summary>
public sealed class ClaimsCurrentUserOptions
{
    /// <summary>
    /// Type of the claim carrying the user identifier. Defaults to
    /// <see cref="ClaimTypes.NameIdentifier" />, which JWT bearer
    /// authentication maps the <c>sub</c> claim to by default; set this
    /// to <c>sub</c> when inbound claim mapping is disabled
    /// (<c>JwtBearerOptions.MapInboundClaims = false</c>), as is common
    /// with OpenID Connect providers such as Keycloak.
    /// </summary>
    public string ClaimType { get; set; } = ClaimTypes.NameIdentifier;
}
