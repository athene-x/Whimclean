namespace Athenex.Whimclean.Identity.Keycloak.Administration;

/// <summary>
/// Outcome of a user search: the user, or <see cref="NotFound" />.
/// </summary>
/// <param name="User">The user, or <see langword="null" /> when none matched.</param>
public sealed record KeycloakUserLookup(KeycloakUser? User)
{
    /// <summary>No user matched.</summary>
    public static KeycloakUserLookup NotFound { get; } = new((KeycloakUser?)null);

    /// <summary>Whether a user matched.</summary>
    public bool Found => User is not null;
}
