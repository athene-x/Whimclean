using Athenex.Whimclean.Identity.Keycloak.Administration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Athenex.Whimclean.Identity.Keycloak;

/// <summary>
/// Service registrations for Keycloak authentication and administration.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers JWT bearer authentication for tokens issued by the
    /// Keycloak realm in the <c>Keycloak</c> section of
    /// <paramref name="configuration" />. See
    /// <see cref="AddKeycloakAuthentication(IServiceCollection, KeycloakOptions)" />.
    /// </summary>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// The section is missing or invalid.
    /// </exception>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = KeycloakOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(sectionName).Get<KeycloakOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{sectionName}' is missing.");

        return services.AddKeycloakAuthentication(options);
    }

    /// <summary>
    /// Registers JWT bearer authentication for tokens issued by a
    /// Keycloak realm, with the conventions those tokens need: inbound
    /// claim mapping off so <c>sub</c> stays <c>sub</c>, the issuer pinned
    /// to the public authority even when discovery runs against an
    /// internal address, and the name and role claims taken from the
    /// options. Also registers authorization services. Pair with
    /// <c>AddClaimsCurrentUser&lt;TUserId&gt;(o =&gt; o.ClaimType = "sub")</c>
    /// or <c>AddLocalUserResolution&lt;TUserId&gt;</c>.
    /// </summary>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="options" /> is invalid.
    /// </exception>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        KeycloakOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Validate() is { } failure)
        {
            throw new InvalidOperationException(failure);
        }

        var authority = options.Authority.TrimEnd('/');

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = authority;
                jwt.MetadataAddress = options.MetadataAddress
                    ?? $"{authority}/.well-known/openid-configuration";
                jwt.Audience = options.Audience;
                jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = authority,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    NameClaimType = options.NameClaimType,
                    RoleClaimType = options.RoleClaimType
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Registers the Keycloak admin client from the <c>KeycloakAdmin</c>
    /// section of <paramref name="configuration" />, validated at
    /// startup. See
    /// <see cref="AddKeycloakAdministration(IServiceCollection, Action{KeycloakAdminOptions})" />.
    /// </summary>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddKeycloakAdministration(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = KeycloakAdminOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<KeycloakAdminOptions>().Bind(configuration.GetSection(sectionName));

        return services.AddKeycloakAdministration(configure: null);
    }

    /// <summary>
    /// Registers <see cref="IKeycloakUserAdministration" /> backed by a
    /// typed <see cref="HttpClient" /> that authenticates with the
    /// configured service account, caching its token through the
    /// singleton <see cref="KeycloakAdminTokenProvider" />. The options
    /// are validated when the host starts.
    /// </summary>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddKeycloakAdministration(
        this IServiceCollection services,
        Action<KeycloakAdminOptions>? configure)
    {
        var builder = services.AddOptions<KeycloakAdminOptions>();

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        builder.ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IValidateOptions<KeycloakAdminOptions>,
            KeycloakAdminOptionsValidator>());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<KeycloakAdminTokenProvider>();
        services.AddHttpClient<IKeycloakUserAdministration, KeycloakUserAdministration>(
            (provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<KeycloakAdminOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            });

        return services;
    }

    private sealed class KeycloakAdminOptionsValidator : IValidateOptions<KeycloakAdminOptions>
    {
        public ValidateOptionsResult Validate(string? name, KeycloakAdminOptions options) =>
            options.Validate() is { } failure
                ? ValidateOptionsResult.Fail(failure)
                : ValidateOptionsResult.Success;
    }
}
