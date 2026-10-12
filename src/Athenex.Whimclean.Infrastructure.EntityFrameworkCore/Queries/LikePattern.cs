namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries;

/// <summary>
/// Builds <c>LIKE</c> patterns from user input, escaping the wildcard
/// characters so a search for <c>50%</c> matches that text rather than
/// everything starting with <c>50</c>. Pass
/// <see cref="EscapeCharacter" /> as the escape argument of
/// <c>EF.Functions.Like</c> (or a provider's case-insensitive variant).
/// </summary>
public static class LikePattern
{
    /// <summary>Escape character the patterns are built with.</summary>
    public const string EscapeCharacter = "\\";

    /// <summary>Pattern matching values that contain <paramref name="value" />.</summary>
    public static string Contains(string value) => $"%{Escape(value)}%";

    /// <summary>Pattern matching values that start with <paramref name="value" />.</summary>
    public static string StartsWith(string value) => $"{Escape(value)}%";

    /// <summary>Pattern matching values that end with <paramref name="value" />.</summary>
    public static string EndsWith(string value) => $"%{Escape(value)}";

    /// <summary>
    /// Escapes <c>\</c>, <c>%</c> and <c>_</c> in <paramref name="value" />
    /// so they match literally.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static string Escape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
