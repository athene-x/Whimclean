using Athenex.Whimclean.Application.Context;

namespace Athenex.Whimclean.Application.Tests.Context;

public sealed class StaticCurrentUserTests
{
    [Fact]
    public void TryGetUserId_AlwaysReturnsConfiguredUser()
    {
        var systemUserId = Guid.NewGuid();
        var currentUser = new StaticCurrentUser<Guid>(systemUserId);

        var resolved = currentUser.TryGetUserId(out var userId);

        Assert.True(resolved);
        Assert.Equal(systemUserId, userId);
    }

    [Fact]
    public void Constructor_RejectsNullUser()
    {
        Assert.Throws<ArgumentNullException>(
            () => new StaticCurrentUser<string>(null!));
    }
}
