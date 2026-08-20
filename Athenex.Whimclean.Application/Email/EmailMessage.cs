namespace Athenex.Whimclean.Application.Email;

public sealed record EmailMessage
{
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

    public string Recipient { get; }
    public string Subject { get; }
    public string? TextBody { get; }
    public string? HtmlBody { get; }

    private static string? NormalizeBody(string? body) =>
        string.IsNullOrWhiteSpace(body) ? null : body;
}
