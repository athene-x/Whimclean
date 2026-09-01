using Athenex.Whimclean.Application.Queries;

namespace Athenex.Whimclean.Application.Tests.Queries;

public sealed class ListQueryOptionsTests
{
    [Fact]
    public void Constructor_UsesDefaultsAndNormalizesSearch()
    {
        var options = new ListQueryOptions("  pending  ");

        Assert.Equal("pending", options.SearchTerm);
        Assert.Equal(ListQueryOptions.DefaultPage, options.Page);
        Assert.Equal(ListQueryOptions.DefaultSize, options.Size);
        Assert.Equal(0, options.Offset);
        Assert.Empty(options.Sort);
        Assert.Empty(options.Filters);
    }

    [Fact]
    public void Constructor_CalculatesOffset()
    {
        var options = new ListQueryOptions(page: 3, size: 25);

        Assert.Equal(50, options.Offset);
    }

    [Fact]
    public void Constructor_MaterializesSortAndFilters()
    {
        var sort = new List<SortTerm> { new("name") };
        var filters = new List<FilterTerm> { new("status", "active") };
        var options = new ListQueryOptions(sort: sort, filters: filters);

        sort.Add(new SortTerm("createdAt"));
        filters.Add(new FilterTerm("tenantId", "tenant"));

        Assert.Single(options.Sort);
        Assert.Single(options.Filters);
    }

    [Fact]
    public void Constructor_WithNullSortElement_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new ListQueryOptions(sort: [null!]));
    }

    [Fact]
    public void Constructor_WithNullFilterElement_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new ListQueryOptions(filters: [null!]));
    }

    [Fact]
    public void Constructor_WhenSizeExceedsMaximum_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ListQueryOptions(size: ListQueryOptions.MaximumSize + 1));
    }

    [Fact]
    public void Constructor_WhenSearchExceedsMaximum_ThrowsArgumentException()
    {
        var search = new string('a', ListQueryOptions.MaximumSearchLength + 1);

        Assert.Throws<ArgumentException>(
            () => new ListQueryOptions(search));
    }

    [Fact]
    public void Constructor_WhenOffsetExceedsSupportedRange_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ListQueryOptions(page: int.MaxValue, size: 2));
    }
}
