namespace Athenex.Whimclean.Auditing;

/// <summary>
/// Turns a command's descriptor and outcome into an audit entry and
/// writes it. Lets <see cref="AuditCommandBehavior{TMessage, TResponse}" />
/// stay independent of the identifier types, which
/// <see cref="AuditLogRecorder{TUserId, TTenantId}" /> supplies.
/// </summary>
public interface IAuditLogRecorder
{
    /// <summary>Records one outcome.</summary>
    /// <param name="descriptor">What the command did.</param>
    /// <param name="result">Whether it succeeded.</param>
    /// <param name="errorCode">Error code or exception type of a failure.</param>
    /// <param name="createdId">
    /// Identifier a creating command returned, used when the descriptor
    /// names no entity id.
    /// </param>
    /// <param name="cancellationToken">
    /// Propagates a request to cancel the write.
    /// </param>
    ValueTask RecordAsync(
        AuditDescriptor descriptor,
        AuditResult result,
        string? errorCode,
        string? createdId,
        CancellationToken cancellationToken = default);
}
