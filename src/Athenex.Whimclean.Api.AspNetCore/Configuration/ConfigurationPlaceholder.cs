using System.Diagnostics.CodeAnalysis;

namespace Athenex.Whimclean.Api.AspNetCore.Configuration;

/// <summary>
/// The marker committed in <c>appsettings.*.json</c> for values that must
/// come from user secrets or environment variables, so a forgotten
/// secret fails at startup with a clear message instead of connecting to
/// nothing.
/// </summary>
public static class ConfigurationPlaceholder
{
    /// <summary>The marker text.</summary>
    public const string Value = "<set-via-user-secrets>";

    /// <summary>
    /// Whether <paramref name="value" /> is missing, blank, or still the
    /// placeholder.
    /// </summary>
    public static bool IsUnset([NotNullWhen(false)] string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        value.Contains(Value, StringComparison.Ordinal);
}
