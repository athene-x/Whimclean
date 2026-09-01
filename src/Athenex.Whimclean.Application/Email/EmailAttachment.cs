namespace Athenex.Whimclean.Application.Email;

/// <summary>
/// One file attached to an <see cref="EmailMessage" />: a file name, its
/// raw content, and the MIME content type the transport declares it with.
/// </summary>
public sealed class EmailAttachment
{
    /// <summary>
    /// Creates the attachment, trimming <paramref name="fileName" /> and
    /// <paramref name="contentType" />.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="fileName" /> or <paramref name="contentType" /> is
    /// <see langword="null" /> or whitespace.
    /// </exception>
    public EmailAttachment(
        string fileName,
        ReadOnlyMemory<byte> content,
        string contentType = "application/octet-stream")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        FileName = fileName.Trim();
        Content = content;
        ContentType = contentType.Trim();
    }

    /// <summary>Trimmed file name presented to the recipient.</summary>
    public string FileName { get; }

    /// <summary>Raw content of the attached file.</summary>
    public ReadOnlyMemory<byte> Content { get; }

    /// <summary>
    /// Trimmed MIME content type; <c>application/octet-stream</c> unless
    /// specified otherwise.
    /// </summary>
    public string ContentType { get; }
}
