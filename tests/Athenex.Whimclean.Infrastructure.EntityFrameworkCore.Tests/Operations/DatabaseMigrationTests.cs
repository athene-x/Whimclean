using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Operations;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Tests.Operations;

public sealed class DatabaseMigrationTests
{
    [Fact]
    public void IsRequested_MatchesTheSwitchCaseInsensitively()
    {
        Assert.True(DatabaseMigration.IsRequested(["--Migrate"]));
        Assert.True(DatabaseMigration.IsRequested(["--urls", "http://+:8080", "--migrate"]));
        Assert.False(DatabaseMigration.IsRequested([]));
        Assert.False(DatabaseMigration.IsRequested(["migrate"]));
    }
}
