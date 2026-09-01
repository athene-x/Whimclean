namespace Athenex.Whimclean.Infrastructure.Auditing;

/// <summary>
/// Lifts a struct user id into its nullable form, for schemas whose actor
/// columns are declared as <typeparamref name="TUserId" />? (e.g. so rows
/// written before auditing existed may leave the column
/// <see langword="null" />).
/// </summary>
/// <typeparam name="TUserId">Value-type user id being lifted.</typeparam>
public sealed class NullableAuditActorResolver<TUserId>
    : IAuditActorResolver<TUserId, TUserId?>
    where TUserId : struct
{
    /// <summary>
    /// Returns <paramref name="userId" /> wrapped as a non-null nullable
    /// value.
    /// </summary>
    public TUserId? Resolve(TUserId userId) => userId;
}
