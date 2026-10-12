using Athenex.Whimclean.Domain.Results;

namespace Athenex.Whimclean.Identity.Keycloak.Administration;

/// <summary>
/// User provisioning through the Keycloak admin API. Every call returns
/// a <see cref="Result" /> with a <see cref="KeycloakErrors" /> error
/// instead of throwing, so handlers can map outcomes to responses.
/// </summary>
public interface IKeycloakUserAdministration
{
    /// <summary>Finds the user whose email equals <paramref name="email" /> exactly.</summary>
    ValueTask<Result<KeycloakUserLookup>> FindUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an enabled user whose user name is their email, with the
    /// required actions to verify the email and set a password.
    /// </summary>
    ValueTask<Result<KeycloakUser>> CreateUserAsync(
        string email,
        string firstName,
        string? lastName,
        CancellationToken cancellationToken = default);

    /// <summary>Whether the user still has to set a password.</summary>
    ValueTask<Result<bool>> RequiresPasswordSetupAsync(
        string subject,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Emails the user a link to verify their email and set a password.
    /// <paramref name="returnPath" /> is appended to the configured
    /// redirect URL.
    /// </summary>
    ValueTask<Result> SendPasswordSetupEmailAsync(
        string subject,
        string? returnPath = null,
        CancellationToken cancellationToken = default);

    /// <summary>Changes the user's first and last name.</summary>
    ValueTask<Result> UpdateUserNameAsync(
        string subject,
        string firstName,
        string? lastName,
        CancellationToken cancellationToken = default);

    /// <summary>Enables or disables the user's ability to sign in.</summary>
    ValueTask<Result> SetUserEnabledAsync(
        string subject,
        bool enabled,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the user; succeeds when they are already gone.</summary>
    ValueTask<Result> DeleteUserAsync(
        string subject,
        CancellationToken cancellationToken = default);
}
