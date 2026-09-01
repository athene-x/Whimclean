using System.Diagnostics.CodeAnalysis;

using Athenex.Whimclean.Application.Context;

namespace Athenex.Whimclean.Application.Tests.Context;

public sealed class CurrentUserExtensionsTests
{
    [Fact]
    public void GetRequiredUserId_WithAuthenticatedUser_ReturnsUserId()
    {
        var systemUserId = Guid.NewGuid();
        var currentUser = new StaticCurrentUser<Guid>(systemUserId);

        Assert.Equal(systemUserId, currentUser.GetRequiredUserId());
    }

    [Fact]
    public void GetRequiredUserId_WithoutUser_ThrowsCurrentUserRequiredException()
    {
        var currentUser = new AnonymousCurrentUser();

        Assert.Throws<CurrentUserRequiredException>(
            () => currentUser.GetRequiredUserId());
    }

    private sealed class AnonymousCurrentUser : ICurrentUser<Guid>
    {
        public bool TryGetUserId([MaybeNullWhen(false)] out Guid userId)
        {
            userId = default;
            return false;
        }
    }
}
