using System.Diagnostics.CodeAnalysis;

namespace Athenex.Whimclean.Application.Context;

public interface ICurrentUser<TUserId>
    where TUserId : notnull
{
    bool TryGetUserId(
        [MaybeNullWhen(false)] out TUserId userId);
}
