namespace Athenex.Whimclean.Identity.Keycloak;

/// <summary>
/// Options for <see cref="DependencyInjection.AddKeycloakAuthentication(Microsoft.Extensions.DependencyInjection.IServiceCollection, KeycloakOptions)" />.
/// </summary>
public sealed class KeycloakOptions
{
    /// <summary>Configuration section the options usually bind from.</summary>
    public const string SectionName = "Keycloak";

    /// <summary>
    /// Public realm URL, such as
    /// <c>https://auth.example.com/realms/app</c>; also the issuer tokens
    /// are validated against.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Client id tokens must carry in their audience.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Discovery document address when the API reaches Keycloak through
    /// an internal hostname that differs from <see cref="Authority" />.
    /// </summary>
    public string? MetadataAddress { get; set; }

    /// <summary>
    /// Whether discovery must use HTTPS. Defaults to
    /// <see langword="true" />; disable only for local development.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Claim used as the principal's name. Defaults to
    /// <c>preferred_username</c>.
    /// </summary>
    public string NameClaimType { get; set; } = "preferred_username";

    /// <summary>
    /// Claim used for roles. Defaults to <c>roles</c>, which a realm role
    /// mapper flattens realm roles into.
    /// </summary>
    public string RoleClaimType { get; set; } = "roles";

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <returns>
    /// A failure message naming the offending setting, or
    /// <see langword="null" /> when the options are valid.
    /// </returns>
    public string? Validate()
    {
        if (!Uri.TryCreate(Authority, UriKind.Absolute, out _))
        {
            return $"{SectionName}:Authority must be an absolute URL.";
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            return $"{SectionName}:Audience is required.";
        }

        if (MetadataAddress is not null && !Uri.TryCreate(MetadataAddress, UriKind.Absolute, out _))
        {
            return $"{SectionName}:MetadataAddress must be an absolute URL.";
        }

        return null;
    }
}
