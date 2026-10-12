namespace Athenex.Whimclean.Domain.Text;

/// <summary>
/// Normalizes user-entered text before it becomes part of a domain value,
/// so names compare consistently and slugs are safe in URLs and DNS
/// labels.
/// </summary>
public static class TextNormalizer
{
    private static readonly char[] SlugSeparators = [' ', '-', '_'];

    /// <summary>
    /// Trims <paramref name="value" /> and collapses every run of
    /// whitespace to a single space.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static string NormalizeWhitespace(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Lower-cases <paramref name="value" /> and turns every run of
    /// whitespace, hyphens and underscores into a single hyphen, with no
    /// leading or trailing hyphen. Other characters pass through, so
    /// callers that need a strict character set validate the result.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static string NormalizeSlug(string value) =>
        string.Join(
            '-',
            NormalizeWhitespace(value)
                .ToLowerInvariant()
                .Split(SlugSeparators, StringSplitOptions.RemoveEmptyEntries));
}
