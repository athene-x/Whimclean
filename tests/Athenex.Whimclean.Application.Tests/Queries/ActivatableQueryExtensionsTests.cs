using Athenex.Whimclean.Application.Queries;
using Athenex.Whimclean.Domain.Behaviors;

namespace Athenex.Whimclean.Application.Tests.Queries;

public sealed class ActivatableQueryExtensionsTests
{
    [Fact]
    public void WhereActive_OnQueryable_ExcludesInactiveRecords()
    {
        var items = new Item[]
        {
            new("A", IsActive: true),
            new("B", IsActive: false),
            new("C", IsActive: true)
        }.AsQueryable();

        var active = items.WhereActive().ToArray();

        Assert.Equal(["A", "C"], active.Select(item => item.Name));
    }

    [Fact]
    public void WhereActive_OnEnumerable_ExcludesInactiveRecords()
    {
        IEnumerable<Item> items =
        [
            new("A", IsActive: false),
            new("B", IsActive: true)
        ];

        var active = items.WhereActive().ToArray();

        var item = Assert.Single(active);
        Assert.Equal("B", item.Name);
    }

    private sealed record Item(string Name, bool IsActive) : IActivatable;
}
