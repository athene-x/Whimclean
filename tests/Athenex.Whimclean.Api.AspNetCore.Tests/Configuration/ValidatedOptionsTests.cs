using Athenex.Whimclean.Api.AspNetCore.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Api.AspNetCore.Tests.Configuration;

public sealed class ValidatedOptionsTests
{
    private sealed class EmailOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
    }

    private static IConfiguration Configuration(string? host) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Host"] = host,
                ["Email:Port"] = "2525"
            })
            .Build();

    private static string? Validate(EmailOptions options) =>
        string.IsNullOrWhiteSpace(options.Host) ? "Email:Host is required." : null;

    [Fact]
    public void AddValidatedOptions_BindsAndAcceptsValidValues()
    {
        var services = new ServiceCollection();
        services.AddValidatedOptions<EmailOptions>(Configuration("smtp.example.com"), "Email", Validate);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<EmailOptions>>().Value;

        Assert.Equal("smtp.example.com", options.Host);
        Assert.Equal(2525, options.Port);
    }

    [Fact]
    public void AddValidatedOptions_ReportsTheFailureMessage()
    {
        var services = new ServiceCollection();
        services.AddValidatedOptions<EmailOptions>(Configuration(null), "Email", Validate);
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<EmailOptions>>().Value);

        Assert.Contains("Email:Host is required.", exception.Message);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("<set-via-user-secrets>", true)]
    [InlineData("Host=db;Password=<set-via-user-secrets>", true)]
    [InlineData("Host=db;Password=real", false)]
    public void ConfigurationPlaceholder_DetectsUnsetValues(string? value, bool expected)
    {
        Assert.Equal(expected, ConfigurationPlaceholder.IsUnset(value));
    }
}
