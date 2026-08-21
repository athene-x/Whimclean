namespace Athenex.Whimclean.Infrastructure.Auditing;

public interface IAuditActorResolver<in TUserId, out TActor>
    where TUserId : notnull
{
    TActor Resolve(TUserId userId);
}
