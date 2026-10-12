namespace Athenex.Whimclean.Infrastructure.Email.Smtp;

/// <summary>
/// Options for <see cref="SmtpEmailSender" />.
/// </summary>
public sealed class SmtpEmailOptions
{
    /// <summary>Configuration section the options usually bind from.</summary>
    public const string SectionName = "Email";

    /// <summary>SMTP server host name.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SMTP server port. Defaults to 587.</summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Address messages are sent from when the message names no sender.
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Display name shown with <see cref="From" />.</summary>
    public string? FromName { get; set; }

    /// <summary>User name for authentication; leave unset for none.</summary>
    public string? User { get; set; }

    /// <summary>Password for authentication.</summary>
    public string? Password { get; set; }

    /// <summary>
    /// Whether to negotiate TLS (STARTTLS) with the server. Defaults to
    /// <see langword="true" />.
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>Send timeout in seconds. Defaults to 30.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <returns>
    /// A failure message naming the offending setting, or
    /// <see langword="null" /> when the options are valid.
    /// </returns>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            return $"{SectionName}:Host is required.";
        }

        if (Port is <= 0 or > 65535)
        {
            return $"{SectionName}:Port must be a valid TCP port.";
        }

        if (string.IsNullOrWhiteSpace(From))
        {
            return $"{SectionName}:From is required.";
        }

        if (TimeoutSeconds <= 0)
        {
            return $"{SectionName}:TimeoutSeconds must be positive.";
        }

        return null;
    }
}
