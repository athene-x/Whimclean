namespace Athenex.Whimclean.Auditing;

/// <summary>
/// An <see cref="IAuditRequestContext" /> that knows nothing, for hosts
/// without a request: workers, seeders, tests.
/// </summary>
public sealed class NullAuditRequestContext : IAuditRequestContext
{
    /// <inheritdoc />
    public string? ActorName => null;

    /// <inheritdoc />
    public string? CorrelationId => null;

    /// <inheritdoc />
    public string? IpAddress => null;

    /// <inheritdoc />
    public string? UserAgent => null;
}
