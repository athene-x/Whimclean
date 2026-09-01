namespace Athenex.Whimclean.Application.Context;

/// <summary>
/// Convenience operations over <see cref="ICurrentUser{TUserId}" />.
/// </summary>
public static class CurrentUserExtensions
{
    /// <summary>
    /// Resolves the current user's identifier, treating its absence as a
    /// boundary violation. Use it wherever an operation cannot proceed
    /// without an attributable user.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="currentUser" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CurrentUserRequiredException">
    /// No authenticated principal is available.
    /// </exception>
    public static TUserId GetRequiredUserId<TUserId>(
        this ICurrentUser<TUserId> currentUser)
        where TUserId : notnull
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        return currentUser.TryGetUserId(out var userId)
            ? userId
            : throw new CurrentUserRequiredException();
    }
}
