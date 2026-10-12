namespace Athenex.Whimclean.Auditing;

/// <summary>
/// Persists an entry on its own, outside the command's unit of work, so a
/// failed command still leaves a trace. The application implements it,
/// typically with a <c>DbContext</c> factory that opens a separate
/// connection.
/// </summary>
/// <typeparam name="TUserId">Type of the local user identifier.</typeparam>
/// <typeparam name="TTenantId">Type of the tenant identifier.</typeparam>
public interface IAuditLogWriter<TUserId, TTenantId>
    where TUserId : struct
    where TTenantId : struct
{
    /// <summary>Writes and commits <paramref name="entry" />.</summary>
    ValueTask WriteAsync(
        AuditLog<TUserId, TTenantId> entry,
        CancellationToken cancellationToken = default);
}
