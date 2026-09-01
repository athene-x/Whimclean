namespace Athenex.Whimclean.Infrastructure.Auditing;

/// <summary>
/// Maps the required current user id to the value stored in the audit
/// actor columns (CreatedBy/UpdatedBy/DeletedBy). Lets the persisted actor
/// type differ from the id type reported by the current-user abstraction.
/// </summary>
/// <typeparam name="TUserId">
/// User id supplied by the current-user abstraction.
/// </typeparam>
/// <typeparam name="TActor">
/// Value persisted in the actor columns.
/// </typeparam>
public interface IAuditActorResolver<in TUserId, out TActor>
    where TUserId : notnull
{
    /// <summary>
    /// Converts the authenticated user's id into the actor value stamped
    /// on audited rows. Called only when a write actually requires an
    /// actor.
    /// </summary>
    TActor Resolve(TUserId userId);
}
