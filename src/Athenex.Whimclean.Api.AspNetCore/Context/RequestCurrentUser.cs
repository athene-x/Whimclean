using System.Diagnostics.CodeAnalysis;
using Athenex.Whimclean.Application.Context;

namespace Athenex.Whimclean.Api.AspNetCore.Context;

/// <summary>
/// A scoped <see cref="ICurrentUser{TUserId}" /> that reports nothing
/// until something in the scope sets it: the
/// <see cref="LocalUserResolutionMiddleware{TUserId}" /> for requests, or
/// <see cref="ICurrentUserInitializer{TUserId}" /> for bootstrap code.
/// Use it when the user identifier is not in the token but in a local
/// table keyed by the token's subject.
/// </summary>
/// <typeparam name="TUserId">Type of the user identifier.</typeparam>
public sealed class RequestCurrentUser<TUserId>
    : ICurrentUser<TUserId>, ICurrentUserInitializer<TUserId>
    where TUserId : notnull
{
    private TUserId? _userId;

    /// <inheritdoc />
    public bool TryGetUserId([MaybeNullWhen(false)] out TUserId userId)
    {
        userId = _userId;
        return !EqualityComparer<TUserId>.Default.Equals(_userId, default);
    }

    /// <inheritdoc />
    public void SetUserId(TUserId userId)
    {
        if (EqualityComparer<TUserId>.Default.Equals(userId, default))
        {
            throw new ArgumentException(
                "The current user ID cannot be the default value.",
                nameof(userId));
        }

        _userId = userId;
    }
}
