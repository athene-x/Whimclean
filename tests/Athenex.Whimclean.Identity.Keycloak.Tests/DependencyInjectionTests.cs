using Athenex.Whimclean.Identity.Keycloak.Administration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Identity.Keycloak.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddKeycloakAuthentication_ConfiguresJwtBearerWithKeycloakConventions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = "https://auth.example.com/realms/app/",
                ["Keycloak:Audience"] = "app-api",
                ["Keycloak:MetadataAddress"] = "http://keycloak:8080/realms/app/.well-known/openid-configuration",
                ["Keycloak:RequireHttpsMetadata"] = "false"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddKeycloakAuthentication(configuration);

        using var provider = services.BuildServiceProvider();
        var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.Equal("https://auth.example.com/realms/app", jwt.Authority);
        Assert.Equal("https://auth.example.com/realms/app", jwt.TokenValidationParameters.ValidIssuer);
        Assert.Equal("app-api", jwt.Audience);
        Assert.Equal("http://keycloak:8080/realms/app/.well-known/openid-configuration", jwt.MetadataAddress);
        Assert.False(jwt.RequireHttpsMetadata);
        Assert.False(jwt.MapInboundClaims);
        Assert.Equal("preferred_username", jwt.TokenValidationParameters.NameClaimType);
        Assert.Equal("roles", jwt.TokenValidationParameters.RoleClaimType);
    }

    [Fact]
    public void AddKeycloakAuthentication_RejectsInvalidOptions()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddKeycloakAuthentication(new KeycloakOptions { Authority = "nope", Audience = "a" }));

        Assert.Contains("Authority", exception.Message);
    }

    [Fact]
    public void AddKeycloakAdministration_RegistersTheClientAndValidatesOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddKeycloakAdministration(options =>
        {
            options.BaseUrl = "http://keycloak:8080";
            options.Realm = "app";
            options.ClientId = "api";
            options.ClientSecret = "secret";
        });

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IKeycloakUserAdministration>();
        Assert.IsType<KeycloakUserAdministration>(client);
        Assert.Same(
            provider.GetRequiredService<KeycloakAdminTokenProvider>(),
            provider.GetRequiredService<KeycloakAdminTokenProvider>());
    }

    [Fact]
    public void AddKeycloakAdministration_FailsOnMissingSecret()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeycloakAdministration(options =>
        {
            options.BaseUrl = "http://keycloak:8080";
            options.Realm = "app";
            options.ClientId = "api";
        });
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<KeycloakAdminOptions>>().Value);

        Assert.Contains("ClientSecret", exception.Message);
    }
}
