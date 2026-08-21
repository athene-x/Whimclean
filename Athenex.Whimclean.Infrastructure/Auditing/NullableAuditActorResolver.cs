namespace Athenex.Whimclean.Infrastructure.Auditing;

public sealed class NullableAuditActorResolver<TUserId>
    : IAuditActorResolver<TUserId, TUserId?>
    where TUserId : struct
{
    public TUserId? Resolve(TUserId userId) => userId;
}
