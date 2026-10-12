using Athenex.Whimclean.Application.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Infrastructure.Email.Smtp;

/// <summary>
/// Service registrations for SMTP email.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="SmtpEmailSender" /> as the
    /// <see cref="IEmailSender" /> (via <c>TryAddScoped</c>, so an existing
    /// sender wins) with options from <paramref name="configure" />,
    /// validated when first resolved. Bind the options from configuration
    /// instead with <c>AddValidatedOptions&lt;SmtpEmailOptions&gt;</c> from
    /// the ASP.NET Core package and call this without a delegate.
    /// </summary>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddSmtpEmailSender(
        this IServiceCollection services,
        Action<SmtpEmailOptions>? configure = null)
    {
        var builder = services.AddOptions<SmtpEmailOptions>();

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IValidateOptions<SmtpEmailOptions>,
            SmtpEmailOptionsValidator>());
        services.TryAddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }

    private sealed class SmtpEmailOptionsValidator : IValidateOptions<SmtpEmailOptions>
    {
        public ValidateOptionsResult Validate(string? name, SmtpEmailOptions options) =>
            options.Validate() is { } failure
                ? ValidateOptionsResult.Fail(failure)
                : ValidateOptionsResult.Success;
    }
}
