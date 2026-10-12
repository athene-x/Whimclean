namespace Athenex.Whimclean.Api.AspNetCore.Context;

/// <summary>
/// Maps the subject an identity provider issued to the application's own
/// user identifier. Implemented by the application over its user table
/// and used by <see cref="LocalUserResolutionMiddleware{TUserId}" />.
/// </summary>
/// <typeparam name="TUserId">Type of the local user identifier.</typeparam>
public interface ILocalUserLookup<TUserId>
    where TUserId : notnull
{
    /// <summary>
    /// Finds the local user registered for
    /// <paramref name="externalSubject" />.
    /// </summary>
    /// <returns>
    /// The local identifier, or <see langword="null" /> (the default
    /// value for a value type) when no local user exists for the subject.
    /// </returns>
    ValueTask<TUserId?> FindByExternalSubjectAsync(
        string externalSubject,
        CancellationToken cancellationToken = default);
}
