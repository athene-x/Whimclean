using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Tests.Queries;

public sealed class LikePatternTests
{
    [Fact]
    public void Contains_EscapesWildcardsAndEscapeCharacter()
    {
        Assert.Equal(@"%50\%\_a\\b%", LikePattern.Contains(@"50%_a\b"));
    }

    [Fact]
    public void StartsWithAndEndsWith_PlaceOneWildcard()
    {
        Assert.Equal("abc%", LikePattern.StartsWith("abc"));
        Assert.Equal("%abc", LikePattern.EndsWith("abc"));
    }
}
