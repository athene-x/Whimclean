namespace Athenex.Whimclean.Api.AspNetCore.Operations;

/// <summary>
/// Options for <see cref="RequestRateLimiting" />: a fixed window per
/// caller.
/// </summary>
public sealed class RequestRateLimitOptions
{
    /// <summary>Configuration section the options bind from.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Requests a caller may make per window. Defaults to 300.
    /// </summary>
    public int PermitLimit { get; set; } = 300;

    /// <summary>Window length in seconds. Defaults to 60.</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Returns a message naming the first invalid setting, or
    /// <see langword="null" /> when the options are valid.
    /// </summary>
    public string? Validate() =>
        PermitLimit <= 0 || WindowSeconds <= 0
            ? $"{SectionName}:PermitLimit and {SectionName}:WindowSeconds must be greater than zero."
            : null;
}
