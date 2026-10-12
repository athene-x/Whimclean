using Athenex.Whimclean.Application.Queries;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Tests.Queries;

public sealed class SortFieldsTests
{
    private sealed record Thing(int Id, string Name, int Rank);

    private static readonly SortFields<Thing> Fields = new()
    {
        { "name", thing => thing.Name },
        { "rank", thing => thing.Rank }
    };

    private static readonly IQueryable<Thing> Things = new[]
    {
        new Thing(1, "b", 2),
        new Thing(2, "a", 2),
        new Thing(3, "c", 1)
    }.AsQueryable();

    [Fact]
    public void Order_AppliesTermsInSequence()
    {
        var ordered = Fields.Order(
            Things,
            [new SortTerm("rank", SortDirection.Descending), new SortTerm("name")]);

        Assert.NotNull(ordered);
        Assert.Equal([2, 1, 3], ordered.Select(thing => thing.Id));
    }

    [Fact]
    public void Order_SkipsUnknownFieldsAndReturnsNullWhenNoneApply()
    {
        Assert.Null(Fields.Order(Things, [new SortTerm("unknown")]));
        Assert.Null(Fields.Order(Things, []));
    }

    [Fact]
    public void Order_WithDefault_FallsBackAndAddsTieBreaker()
    {
        var ordered = Fields.Order(
            Things,
            [],
            thing => thing.Rank,
            thing => thing.Id,
            SortDirection.Descending);

        Assert.Equal([1, 2, 3], ordered.Select(thing => thing.Id));
    }

    [Fact]
    public void Add_RejectsDuplicateNames()
    {
        var fields = new SortFields<Thing> { { "name", thing => thing.Name } };

        Assert.Throws<ArgumentException>(() => fields.Add("name", thing => thing.Id));
        Assert.Equal(["name"], fields);
        Assert.True(fields.Contains("name"));
    }
}
