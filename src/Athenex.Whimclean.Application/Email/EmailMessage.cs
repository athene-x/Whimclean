namespace Athenex.Whimclean.Application.Email;

/// <summary>
/// Immutable outbound email handed to <see cref="IEmailSender" />: one
/// recipient, a subject, and at least one of a plain-text or HTML body.
/// All values are trimmed; blank bodies are normalized to
/// <see langword="null" />.
/// </summary>
public sealed record EmailMessage
{
    /// <summary>
    /// Creates the message, trimming <paramref name="recipient" /> and
    /// <paramref name="subject" /> and normalizing blank bodies to
    /// <see langword="null" />.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="recipient" /> or <paramref name="subject" /> is
    /// <see langword="null" /> or whitespace, or both
    /// <paramref name="textBody" /> and <paramref name="htmlBody" /> are
    /// missing or blank.
    /// </exception>
    public EmailMessage(
        string recipient,
        string subject,
        string? textBody = null,
        string? htmlBody = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        if (string.IsNullOrWhiteSpace(textBody) &&
            string.IsNullOrWhiteSpace(htmlBody))
        {
            throw new ArgumentException(
                "An email must contain a text or HTML body.",
                nameof(textBody));
        }

        Recipient = recipient.Trim();
        Subject = subject.Trim();
        TextBody = NormalizeBody(textBody);
        HtmlBody = NormalizeBody(htmlBody);
    }

    /// <summary>Trimmed address the message is delivered to.</summary>
    public string Recipient { get; }

    /// <summary>Trimmed subject line; never empty.</summary>
    public string Subject { get; }

    /// <summary>
    /// Plain-text body, or <see langword="null" /> when only an HTML body
    /// was supplied.
    /// </summary>
    public string? TextBody { get; }

    /// <summary>
    /// HTML body, or <see langword="null" /> when only a plain-text body
    /// was supplied. At least one of <see cref="TextBody" /> and
    /// <see cref="HtmlBody" /> is always present.
    /// </summary>
    public string? HtmlBody { get; }

    private static string? NormalizeBody(string? body) =>
        string.IsNullOrWhiteSpace(body) ? null : body;
}
