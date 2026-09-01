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

        Assert.Equal("member@example.com", message.Recipient);
        Assert.Equal("Welcome", message.Subject);
        Assert.Equal("Text body", message.TextBody);
        Assert.Null(message.HtmlBody);
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
}
