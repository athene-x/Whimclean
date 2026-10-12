namespace Athenex.Whimclean.Application.Context;

/// <summary>
/// Lets code that runs outside an authenticated request (startup
/// bootstrap, seeders, a command-line task) name the user its writes are
/// attributed to. Request handling never needs it: the host's user
/// resolution sets the current user.
/// </summary>
/// <typeparam name="TUserId">Type of the user identifier.</typeparam>
public interface ICurrentUserInitializer<in TUserId>
    where TUserId : notnull
{
    /// <summary>
    /// Sets the user every subsequent
    /// <see cref="ICurrentUser{TUserId}.TryGetUserId" /> in this scope
    /// reports.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="userId" /> is the default value of
    /// <typeparamref name="TUserId" />.
    /// </exception>
    void SetUserId(TUserId userId);
}
