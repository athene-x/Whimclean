using Athenex.Whimclean.Domain.Entities;

namespace Athenex.Whimclean.Domain.Tests.Entities;

public sealed class EntityTests
{
    [Fact]
    public void Constructor_WithDefaultId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TestEntity(Guid.Empty));
    }

    [Fact]
    public void Entities_WithSameTypeAndId_AreEqual()
    {
        var id = Guid.NewGuid();
        var first = new TestEntity(id);
        var second = new TestEntity(id);

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Entities_WithDifferentRuntimeTypes_AreNotEqual()
    {
        var id = Guid.NewGuid();

        Assert.NotEqual<Entity<Guid>>(new TestEntity(id), new OtherEntity(id));
    }

    [Fact]
    public void TransientEntities_AreEqualOnlyByReference()
    {
        var first = new TestEntity();
        var second = new TestEntity();

        Assert.Equal(first, first);
        Assert.NotEqual(first, second);
    }

    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id)
        {
        }

        public TestEntity()
        {
        }
    }

    private sealed class OtherEntity(Guid id) : Entity<Guid>(id);
}
