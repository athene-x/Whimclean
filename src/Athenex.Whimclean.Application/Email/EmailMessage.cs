namespace Athenex.Whimclean.Application.Email;

/// <summary>
/// Immutable outbound email handed to <see cref="IEmailSender" />: one or
/// more recipients, a subject, at least one of a plain-text or HTML body,
/// and optionally CC/BCC recipients, sender addresses, and attachments.
/// All addresses and the subject are trimmed; blank bodies are normalized
/// to <see langword="null" />.
/// </summary>
public sealed class EmailMessage
{
    /// <summary>
    /// Creates a single-recipient message with no CC, BCC, sender
    /// overrides, or attachments.
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
        : this(ToRecipients(recipient), subject, textBody, htmlBody)
    {
    }

    /// <summary>
    /// Creates the message, trimming every address and the subject and
    /// normalizing blank bodies to <see langword="null" />. Leave
    /// <paramref name="from" /> unset to use the transport's configured
    /// sender.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="recipients" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="recipients" /> is empty; an element of
    /// <paramref name="recipients" />, <paramref name="cc" />, or
    /// <paramref name="bcc" /> is <see langword="null" /> or whitespace;
    /// an element of <paramref name="attachments" /> is
    /// <see langword="null" />; <paramref name="subject" /> is
    /// <see langword="null" /> or whitespace; <paramref name="from" /> or
    /// <paramref name="replyTo" /> is non-null but whitespace; or both
    /// <paramref name="textBody" /> and <paramref name="htmlBody" /> are
    /// missing or blank.
    /// </exception>
    public EmailMessage(
        IEnumerable<string> recipients,
        string subject,
        string? textBody = null,
        string? htmlBody = null,
        IEnumerable<string>? cc = null,
        IEnumerable<string>? bcc = null,
        string? from = null,
        string? replyTo = null,
        IEnumerable<EmailAttachment>? attachments = null)
    {
        ArgumentNullException.ThrowIfNull(recipients);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        var normalizedRecipients = NormalizeAddresses(
            recipients,
            nameof(recipients));

        if (normalizedRecipients.Length == 0)
        {
            throw new ArgumentException(
                "An email must contain at least one recipient.",
                nameof(recipients));
        }

        if (string.IsNullOrWhiteSpace(textBody) &&
            string.IsNullOrWhiteSpace(htmlBody))
        {
            throw new ArgumentException(
                "An email must contain a text or HTML body.",
                nameof(textBody));
        }

        var materializedAttachments = attachments?.ToArray() ?? [];

        if (Array.Exists(
                materializedAttachments,
                attachment => attachment is null))
        {
            throw new ArgumentException(
                "The attachments cannot contain a null element.",
                nameof(attachments));
        }

        Recipients = Array.AsReadOnly(normalizedRecipients);
        Subject = subject.Trim();
        TextBody = NormalizeBody(textBody);
        HtmlBody = NormalizeBody(htmlBody);
        Cc = Array.AsReadOnly(NormalizeAddresses(cc, nameof(cc)));
        Bcc = Array.AsReadOnly(NormalizeAddresses(bcc, nameof(bcc)));
        From = NormalizeOptionalAddress(from, nameof(from));
        ReplyTo = NormalizeOptionalAddress(replyTo, nameof(replyTo));
        Attachments = Array.AsReadOnly(materializedAttachments);
    }

    /// <summary>
    /// Trimmed addresses the message is delivered to; never empty.
    /// </summary>
    public IReadOnlyList<string> Recipients { get; }

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

    /// <summary>
    /// Trimmed carbon-copy addresses; empty when none were supplied.
    /// </summary>
    public IReadOnlyList<string> Cc { get; }

    /// <summary>
    /// Trimmed blind-carbon-copy addresses; empty when none were supplied.
    /// </summary>
    public IReadOnlyList<string> Bcc { get; }

    /// <summary>
    /// Trimmed sender address, or <see langword="null" /> to use the
    /// transport's configured sender.
    /// </summary>
    public string? From { get; }

    /// <summary>
    /// Trimmed address replies are directed to, or <see langword="null" />
    /// when replies go to the sender.
    /// </summary>
    public string? ReplyTo { get; }

    /// <summary>
    /// Read-only attached files; empty when none were supplied.
    /// </summary>
    public IReadOnlyList<EmailAttachment> Attachments { get; }

    private static string[] ToRecipients(string recipient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);

        return [recipient];
    }

    private static string[] NormalizeAddresses(
        IEnumerable<string>? addresses,
        string parameterName)
    {
        var materialized = addresses?.ToArray() ?? [];

        if (Array.Exists(materialized, string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "The addresses cannot contain a null or whitespace element.",
                parameterName);
        }

        return [.. materialized.Select(address => address.Trim())];
    }

    private static string? NormalizeOptionalAddress(
        string? address,
        string parameterName)
    {
        if (address is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException(
                "The address cannot be whitespace.",
                parameterName);
        }

        return address.Trim();
    }

    private static string? NormalizeBody(string? body) =>
        string.IsNullOrWhiteSpace(body) ? null : body;
}
