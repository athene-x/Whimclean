namespace Athenex.Whimclean.Application.Email;

/// <summary>
/// Transport-agnostic contract for sending outbound email. Implementations
/// bind it to a concrete provider (SMTP, an HTTP API, a test double);
/// application code composes an <see cref="EmailMessage" /> and stays
/// unaware of the transport.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends the message through the underlying transport. Completes when
    /// the transport has accepted the message, which does not guarantee
    /// delivery to the recipient.
    /// </summary>
    Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default);
}
