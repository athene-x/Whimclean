using System.Diagnostics.CodeAnalysis;

namespace Athenex.Whimclean.Application.Context;

/// <summary>
/// Supplies the identity of the user on whose behalf the current operation
/// runs, so writes can be attributed to them.
/// </summary>
/// <typeparam name="TUserId">Type of the user identifier.</typeparam>
public interface ICurrentUser<TUserId>
    where TUserId : notnull
{
    /// <summary>
    /// Attempts to resolve the current user's identifier. Returns
    /// <see langword="false" /> when no authenticated principal is
    /// available, in which case <paramref name="userId" /> must not be
    /// read.
    /// </summary>
    bool TryGetUserId(
        [MaybeNullWhen(false)] out TUserId userId);
}
