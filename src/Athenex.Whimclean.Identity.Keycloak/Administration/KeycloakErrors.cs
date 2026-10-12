using Athenex.Whimclean.Domain.Results;

namespace Athenex.Whimclean.Identity.Keycloak.Administration;

/// <summary>
/// Errors <see cref="IKeycloakUserAdministration" /> returns.
/// </summary>
public static class KeycloakErrors
{
    /// <summary>The admin API could not be reached or answered with an error.</summary>
    public static readonly Error Unavailable = Error.Failure(
        "IdentityProvider.Unavailable",
        "The identity provider could not complete the request.");

    /// <summary>A user with the email already exists.</summary>
    public static readonly Error UserAlreadyExists = Error.Conflict(
        "IdentityProvider.UserAlreadyExists",
        "A user with this email already exists in the identity provider.");

    /// <summary>The user no longer exists.</summary>
    public static readonly Error UserNotFound = Error.NotFound(
        "IdentityProvider.UserNotFound",
        "The user no longer exists in the identity provider.");

    /// <summary>The password setup email could not be sent.</summary>
    public static readonly Error PasswordSetupEmailNotSent = Error.Failure(
        "IdentityProvider.PasswordSetupEmailNotSent",
        "The identity provider could not send the password setup email.");
}
