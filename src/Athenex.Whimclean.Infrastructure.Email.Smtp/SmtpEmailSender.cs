using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Runtime.InteropServices;
using Athenex.Whimclean.Application.Email;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Infrastructure.Email.Smtp;

/// <summary>
/// Sends <see cref="EmailMessage" />s through an SMTP server with
/// <see cref="SmtpClient" />. A message with both bodies is sent as a
/// multipart alternative; attachments keep their declared content type.
/// </summary>
/// <param name="options">Server, credentials and default sender.</param>
public sealed class SmtpEmailSender(IOptions<SmtpEmailOptions> options) : IEmailSender
{
    private readonly SmtpEmailOptions _options = options.Value;

    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var mail = CreateMailMessage(message, _options);
        using var client = CreateClient(_options);

        await client.SendMailAsync(mail, cancellationToken);
    }

    /// <summary>
    /// Maps <paramref name="message" /> to a <see cref="MailMessage" />,
    /// using the configured sender when the message names none.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="message" /> or <paramref name="options" /> is
    /// <see langword="null" />.
    /// </exception>
    public static MailMessage CreateMailMessage(EmailMessage message, SmtpEmailOptions options)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);

        var mail = new MailMessage
        {
            From = message.From is { } from
                ? new MailAddress(from)
                : new MailAddress(options.From, options.FromName),
            Subject = message.Subject
        };

        foreach (var recipient in message.Recipients)
        {
            mail.To.Add(new MailAddress(recipient));
        }

        foreach (var cc in message.Cc)
        {
            mail.CC.Add(new MailAddress(cc));
        }

        foreach (var bcc in message.Bcc)
        {
            mail.Bcc.Add(new MailAddress(bcc));
        }

        if (message.ReplyTo is { } replyTo)
        {
            mail.ReplyToList.Add(new MailAddress(replyTo));
        }

        if (message.TextBody is { } text && message.HtmlBody is { } html)
        {
            mail.Body = text;
            mail.IsBodyHtml = false;
            mail.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));
        }
        else if (message.HtmlBody is { } htmlOnly)
        {
            mail.Body = htmlOnly;
            mail.IsBodyHtml = true;
        }
        else
        {
            mail.Body = message.TextBody;
            mail.IsBodyHtml = false;
        }

        foreach (var attachment in message.Attachments)
        {
            mail.Attachments.Add(new Attachment(
                AsStream(attachment.Content),
                attachment.FileName,
                attachment.ContentType));
        }

        return mail;
    }

    /// <summary>
    /// Wraps array-backed content without copying; the attachment only
    /// reads the stream.
    /// </summary>
    private static MemoryStream AsStream(ReadOnlyMemory<byte> content) =>
        MemoryMarshal.TryGetArray(content, out var segment)
            ? new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false)
            : new MemoryStream(content.ToArray());

    private static SmtpClient CreateClient(SmtpEmailOptions options)
    {
        var client = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl,
            Timeout = options.TimeoutSeconds * 1000
        };

        if (!string.IsNullOrWhiteSpace(options.User))
        {
            client.Credentials = new NetworkCredential(options.User, options.Password);
        }

        return client;
    }
}
