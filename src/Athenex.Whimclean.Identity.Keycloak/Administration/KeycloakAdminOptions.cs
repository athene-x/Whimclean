namespace Athenex.Whimclean.Identity.Keycloak.Administration;

/// <summary>
/// Options for <see cref="KeycloakUserAdministration" />: where the admin
/// API is, which realm to manage, and the service account to call it
/// with.
/// </summary>
public sealed class KeycloakAdminOptions
{
    /// <summary>Configuration section the options usually bind from.</summary>
    public const string SectionName = "KeycloakAdmin";

    /// <summary>
    /// Keycloak base URL as the API reaches it, such as
    /// <c>http://keycloak:8080</c>.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Realm whose users are managed.</summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// Confidential client with the <c>manage-users</c> and
    /// <c>view-users</c> service-account roles.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Secret of <see cref="ClientId" />.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Public client the password-setup email's link signs in through;
    /// required when <see cref="PasswordSetupRedirectUrl" /> is set.
    /// </summary>
    public string? PasswordSetupClientId { get; set; }

    /// <summary>
    /// Where Keycloak sends the user after they set their password; a
    /// request may append a path to it.
    /// </summary>
    public string? PasswordSetupRedirectUrl { get; set; }

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <returns>
    /// A failure message naming the offending setting, or
    /// <see langword="null" /> when the options are valid.
    /// </returns>
    public string? Validate()
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
        {
            return $"{SectionName}:BaseUrl must be an absolute URL.";
        }

        if (string.IsNullOrWhiteSpace(Realm))
        {
            return $"{SectionName}:Realm is required.";
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            return $"{SectionName}:ClientId is required.";
        }

        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            return $"{SectionName}:ClientSecret is required.";
        }

        if (!string.IsNullOrWhiteSpace(PasswordSetupRedirectUrl))
        {
            if (!Uri.TryCreate(PasswordSetupRedirectUrl, UriKind.Absolute, out _))
            {
                return $"{SectionName}:PasswordSetupRedirectUrl must be an absolute URL.";
            }

            if (string.IsNullOrWhiteSpace(PasswordSetupClientId))
            {
                return $"{SectionName}:PasswordSetupClientId is required when PasswordSetupRedirectUrl is set.";
            }
        }

        return null;
    }
}
