using Athenex.Whimclean.Infrastructure.Seeding;

namespace Athenex.Whimclean.Infrastructure.Tests.Seeding;

public sealed class DeterministicGuidTests
{
    [Fact]
    public void Create_IsStableForTheSameInput()
    {
        var parent = Guid.Parse("8e5b3f0a-1d2c-4b7e-9f10-2a3b4c5d6e7f");

        Assert.Equal(DeterministicGuid.Create(parent, "owner"), DeterministicGuid.Create(parent, "owner"));
        Assert.Equal(DeterministicGuid.Create("owner"), DeterministicGuid.Create(Guid.Empty, "owner"));
    }

    [Fact]
    public void Create_DiffersByParentAndKey()
    {
        var parent = Guid.NewGuid();

        Assert.NotEqual(DeterministicGuid.Create(parent, "owner"), DeterministicGuid.Create(parent, "member"));
        Assert.NotEqual(DeterministicGuid.Create(parent, "owner"), DeterministicGuid.Create(Guid.NewGuid(), "owner"));
        Assert.NotEqual(Guid.Empty, DeterministicGuid.Create(parent, "owner"));
    }

    [Fact]
    public void Create_RejectsBlankKey()
    {
        Assert.Throws<ArgumentException>(() => DeterministicGuid.Create(Guid.NewGuid(), " "));
    }
}
