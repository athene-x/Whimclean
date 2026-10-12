using System.Text.Json;

namespace Athenex.Whimclean.Auditing;

/// <summary>
/// Serializes the JSON documents stored on an audit entry with web
/// defaults (camelCase) and nulls kept, so a change to <c>null</c> stays
/// visible.
/// </summary>
public static class AuditJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Serializes <paramref name="value" />, or returns
    /// <see langword="null" /> for a <see langword="null" /> value.
    /// </summary>
    public static string? Serialize<T>(T? value)
        where T : class =>
        value is null ? null : JsonSerializer.Serialize(value, Options);
}
