namespace Athenex.Whimclean.Identity.Keycloak.Administration;

/// <summary>
/// A user as the Keycloak admin API reports it.
/// </summary>
/// <param name="Subject">Keycloak user id, the <c>sub</c> of their tokens.</param>
/// <param name="Email">Email address.</param>
/// <param name="FirstName">First name, or the user name when Keycloak has no names.</param>
/// <param name="LastName">Last name, if any.</param>
public sealed record KeycloakUser(
    string Subject,
    string Email,
    string FirstName,
    string? LastName);
