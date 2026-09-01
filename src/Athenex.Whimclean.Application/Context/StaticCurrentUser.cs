using System.Diagnostics.CodeAnalysis;

namespace Athenex.Whimclean.Application.Context;

/// <summary>
/// An <see cref="ICurrentUser{TUserId}" /> that always reports a fixed user.
/// Register it in hosts that have no authenticated request — background
/// workers, seeders, migrations — so writes are attributed to a designated
/// system account instead of being rejected.
/// </summary>
public sealed class StaticCurrentUser<TUserId> : ICurrentUser<TUserId>
    where TUserId : notnull
{
    private readonly TUserId _userId;

    /// <summary>
    /// Creates the provider with the fixed identifier that every call to
    /// <see cref="TryGetUserId" /> will report.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="userId" /> is <see langword="null" />.
    /// </exception>
    public StaticCurrentUser(TUserId userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        _userId = userId;
    }

    /// <summary>
    /// Always succeeds, yielding the fixed identifier supplied at
    /// construction.
    /// </summary>
    public bool TryGetUserId([MaybeNullWhen(false)] out TUserId userId)
    {
        userId = _userId;
        return true;
    }
}
