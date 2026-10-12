using System.Net.Mime;
using Athenex.Whimclean.Application.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Infrastructure.Email.Smtp.Tests;

public sealed class SmtpEmailSenderTests
{
    private static readonly SmtpEmailOptions Options = new()
    {
        Host = "smtp.example.com",
        From = "noreply@example.com",
        FromName = "Example"
    };

    [Fact]
    public void CreateMailMessage_MapsAddressesBodiesAndAttachments()
    {
        var message = new EmailMessage(
            recipients: ["to@example.com", "to2@example.com"],
            subject: "Hello",
            textBody: "plain",
            htmlBody: "<p>html</p>",
            cc: ["cc@example.com"],
            bcc: ["bcc@example.com"],
            replyTo: "reply@example.com",
            attachments: [new EmailAttachment("a.txt", "abc"u8.ToArray(), MediaTypeNames.Text.Plain)]);

        using var mail = SmtpEmailSender.CreateMailMessage(message, Options);

        Assert.Equal("noreply@example.com", mail.From!.Address);
        Assert.Equal("Example", mail.From.DisplayName);
        Assert.Equal(["to@example.com", "to2@example.com"], mail.To.Select(a => a.Address));
        Assert.Equal(["cc@example.com"], mail.CC.Select(a => a.Address));
        Assert.Equal(["bcc@example.com"], mail.Bcc.Select(a => a.Address));
        Assert.Equal(["reply@example.com"], mail.ReplyToList.Select(a => a.Address));
        Assert.Equal("plain", mail.Body);
        Assert.False(mail.IsBodyHtml);
        Assert.Single(mail.AlternateViews);
        var attachment = Assert.Single(mail.Attachments);
        Assert.Equal("a.txt", attachment.Name);
        Assert.Equal(MediaTypeNames.Text.Plain, attachment.ContentType.MediaType);
    }

    [Fact]
    public void CreateMailMessage_UsesTheMessageSenderAndHtmlOnlyBody()
    {
        var message = new EmailMessage("to@example.com", "Hi", htmlBody: "<b>x</b>");
        var withFrom = new EmailMessage(["to@example.com"], "Hi", textBody: "x", from: "me@example.com");

        using var htmlMail = SmtpEmailSender.CreateMailMessage(message, Options);
        using var fromMail = SmtpEmailSender.CreateMailMessage(withFrom, Options);

        Assert.True(htmlMail.IsBodyHtml);
        Assert.Equal("<b>x</b>", htmlMail.Body);
        Assert.Empty(htmlMail.AlternateViews);
        Assert.Equal("me@example.com", fromMail.From!.Address);
    }

    [Theory]
    [InlineData("", 587, "from@example.com", 30, "Host")]
    [InlineData("h", 0, "from@example.com", 30, "Port")]
    [InlineData("h", 587, " ", 30, "From")]
    [InlineData("h", 587, "from@example.com", 0, "Timeout")]
    public void Validate_NamesTheOffendingSetting(string host, int port, string from, int timeout, string expected)
    {
        var options = new SmtpEmailOptions { Host = host, Port = port, From = from, TimeoutSeconds = timeout };

        Assert.Contains(expected, options.Validate());
    }

    [Fact]
    public void AddSmtpEmailSender_RegistersTheSenderAndValidatesOptions()
    {
        var services = new ServiceCollection();
        services.AddSmtpEmailSender(options => options.Host = "smtp.example.com");
        using var provider = services.BuildServiceProvider();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IEmailSender) &&
            descriptor.ImplementationType == typeof(SmtpEmailSender));
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<SmtpEmailOptions>>().Value);
        Assert.Contains("From", exception.Message);
    }
}
