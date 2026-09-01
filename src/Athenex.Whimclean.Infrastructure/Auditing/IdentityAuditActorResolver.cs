namespace Athenex.Whimclean.Infrastructure.Auditing;

/// <summary>
/// The identity mapping: the user id is stored in the actor columns
/// unchanged. Use when the actor column type equals the user id type.
/// </summary>
/// <typeparam name="TActor">
/// Type serving as both the user id and the stored actor value.
/// </typeparam>
public sealed class IdentityAuditActorResolver<TActor>
    : IAuditActorResolver<TActor, TActor>
    where TActor : notnull
{
    /// <summary>Returns <paramref name="userId" /> unchanged.</summary>
    public TActor Resolve(TActor userId) => userId;
}
