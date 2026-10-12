using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Api.AspNetCore.Configuration;

/// <summary>
/// Binds an options class to a configuration section and validates it
/// when the host starts, so a misconfigured deployment fails to boot
/// instead of failing on the first request that needs the value.
/// </summary>
public static class ValidatedOptions
{
    /// <summary>
    /// Binds <typeparamref name="TOptions" /> to
    /// <paramref name="sectionName" /> and runs
    /// <paramref name="validate" /> at startup; it returns a failure
    /// message, or <see langword="null" /> when the options are valid.
    /// </summary>
    /// <returns>The options builder, for further configuration.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configuration" /> or <paramref name="validate" />
    /// is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="sectionName" /> is <see langword="null" /> or
    /// whitespace.
    /// </exception>
    public static OptionsBuilder<TOptions> AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName,
        Func<TOptions, string?> validate)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        ArgumentNullException.ThrowIfNull(validate);

        services.AddSingleton<IValidateOptions<TOptions>>(
            new DelegateValidateOptions<TOptions>(validate));

        return services
            .AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateOnStart();
    }

    private sealed class DelegateValidateOptions<TOptions>(Func<TOptions, string?> validate)
        : IValidateOptions<TOptions>
        where TOptions : class
    {
        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
            var failure = validate(options);

            return failure is null
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failure);
        }
    }
}
