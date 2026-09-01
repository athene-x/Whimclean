using Athenex.Whimclean.Application.Email;

namespace Athenex.Whimclean.Application.Tests.Email;

public sealed class EmailMessageTests
{
    [Fact]
    public void Constructor_NormalizesRecipientSubjectAndBodies()
    {
        var message = new EmailMessage(
            "  member@example.com  ",
            "  Welcome  ",
            "Text body",
            "   ");

        Assert.Equal(["member@example.com"], message.Recipients);
        Assert.Equal("Welcome", message.Subject);
        Assert.Equal("Text body", message.TextBody);
        Assert.Null(message.HtmlBody);
        Assert.Empty(message.Cc);
        Assert.Empty(message.Bcc);
        Assert.Null(message.From);
        Assert.Null(message.ReplyTo);
        Assert.Empty(message.Attachments);
    }

    [Fact]
    public void Constructor_NormalizesAllAddressesAndMaterializesAttachments()
    {
        var attachment = new EmailAttachment(
            "  invoice.pdf  ",
            new byte[] { 1, 2, 3 },
            "  application/pdf  ");
        var message = new EmailMessage(
            recipients: ["  owner@example.com  ", "billing@example.com"],
            subject: "Invoice",
            textBody: "Attached.",
            cc: ["  manager@example.com  "],
            bcc: ["audit@example.com"],
            from: "  noreply@example.com  ",
            replyTo: "  support@example.com  ",
            attachments: [attachment]);

        Assert.Equal(
            ["owner@example.com", "billing@example.com"],
            message.Recipients);
        Assert.Equal(["manager@example.com"], message.Cc);
        Assert.Equal(["audit@example.com"], message.Bcc);
        Assert.Equal("noreply@example.com", message.From);
        Assert.Equal("support@example.com", message.ReplyTo);

        var materialized = Assert.Single(message.Attachments);
        Assert.Equal("invoice.pdf", materialized.FileName);
        Assert.Equal("application/pdf", materialized.ContentType);
        Assert.Equal(
            new byte[] { 1, 2, 3 },
            materialized.Content.ToArray());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("  ", "\t")]
    public void Constructor_WithoutUsableBody_ThrowsArgumentException(
        string? textBody,
        string? htmlBody)
    {
        Assert.Throws<ArgumentException>(
            () => new EmailMessage(
                "member@example.com",
                "Subject",
                textBody,
                htmlBody));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_WithInvalidRecipient_ThrowsArgumentException(
        string? recipient)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new EmailMessage(recipient!, "Subject", "Body"));
    }

    [Fact]
    public void Constructor_WithoutRecipients_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new EmailMessage([], "Subject", "Body"));
    }

    [Fact]
    public void Constructor_WithBlankCcElement_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new EmailMessage(
                ["member@example.com"],
                "Subject",
                "Body",
                cc: ["  "]));
    }

    [Fact]
    public void Constructor_WithBlankReplyTo_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new EmailMessage(
                ["member@example.com"],
                "Subject",
                "Body",
                replyTo: "  "));
    }

    [Fact]
    public void Constructor_WithNullAttachmentElement_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new EmailMessage(
                ["member@example.com"],
                "Subject",
                "Body",
                attachments: [null!]));
    }

    [Fact]
    public void Attachment_WithBlankFileName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new EmailAttachment("  ", new byte[] { 1 }));
    }
}
