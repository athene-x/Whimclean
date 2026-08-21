namespace Athenex.Whimclean.Infrastructure.Auditing;

public sealed class IdentityAuditActorResolver<TActor>
    : IAuditActorResolver<TActor, TActor>
    where TActor : notnull
{
    public TActor Resolve(TActor userId) => userId;
}
