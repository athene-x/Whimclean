namespace Athenex.Whimclean.Auditing;

/// <summary>
/// What the transport knows about the caller. Everything is optional: a
/// background operation has none of it. The ASP.NET Core package supplies
/// an implementation over the current request.
/// </summary>
public interface IAuditRequestContext
{
    /// <summary>Display name of the caller, such as a token's name claim.</summary>
    string? ActorName { get; }

    /// <summary>Identifier tying the entry to request logs.</summary>
    string? CorrelationId { get; }

    /// <summary>Remote address of the caller.</summary>
    string? IpAddress { get; }

    /// <summary>User agent of the caller.</summary>
    string? UserAgent { get; }
}
