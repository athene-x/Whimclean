namespace Athenex.Whimclean.Auditing;

/// <summary>
/// What the transport knew about the caller when the command ran.
/// </summary>
/// <param name="CorrelationId">Identifier tying the entry to request logs.</param>
/// <param name="IpAddress">Remote address of the caller.</param>
/// <param name="UserAgent">User agent of the caller.</param>
public sealed record AuditRequestInfo(
    string? CorrelationId = null,
    string? IpAddress = null,
    string? UserAgent = null)
{
    /// <summary>No request information; used for background work.</summary>
    public static AuditRequestInfo None { get; } = new();
}
