namespace Athenex.Whimclean.Application.Context;

/// <summary>
/// Ambient, transport-agnostic metadata about the request or operation
/// currently being processed.
/// </summary>
public interface ICurrentRequestContext
{
    /// <summary>
    /// Identifier that ties together all logs and operations belonging to
    /// the current request.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Originating client address, or <see langword="null" /> when the
    /// caller's address is unknown or not applicable (e.g. a background
    /// host).
    /// </summary>
    string? IpAddress { get; }
}
