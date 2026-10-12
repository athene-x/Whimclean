namespace Athenex.Whimclean.Auditing;

/// <summary>
/// The actor an audit entry is attributed to.
/// </summary>
/// <typeparam name="TUserId">Type of the local user identifier.</typeparam>
/// <param name="Id">
/// The local user identifier, or <see langword="null" /> for the system
/// or for a caller without a local user.
/// </param>
/// <param name="Name">Display name recorded on the entry; never blank.</param>
/// <param name="Type">Kind of actor.</param>
public sealed record AuditActor<TUserId>(
    TUserId? Id,
    string Name,
    AuditActorType Type)
    where TUserId : struct
{
    /// <summary>Name recorded for <see cref="System" />.</summary>
    public const string SystemName = "system";

    /// <summary>Name recorded for <see cref="Anonymous" />.</summary>
    public const string AnonymousName = "anonymous";

    /// <summary>The application acting on its own behalf.</summary>
    public static AuditActor<TUserId> System { get; } = new(null, SystemName, AuditActorType.System);

    /// <summary>A caller that could not be identified.</summary>
    public static AuditActor<TUserId> Anonymous { get; } = new(null, AnonymousName, AuditActorType.User);
}
