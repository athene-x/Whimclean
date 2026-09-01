using Athenex.Whimclean.Application.Queries;

namespace Athenex.Whimclean.Application.Tests.Queries;

public sealed class QueryTermTests
{
    [Fact]
    public void SortTerm_NormalizesField()
    {
        var term = new SortTerm("  createdAt  ", SortDirection.Descending);

        Assert.Equal("createdAt", term.Field);
        Assert.Equal(SortDirection.Descending, term.Direction);
    }

    [Fact]
    public void FilterTerm_NormalizesFieldAndPreservesValueVerbatim()
    {
        var term = new FilterTerm(
            "  status  ",
            "  active  ",
            FilterOperator.Equal);

        Assert.Equal("status", term.Field);
        Assert.Equal("  active  ", term.Value);
        Assert.Equal(FilterOperator.Equal, term.Operator);
    }

    [Fact]
    public void FilterTerm_WithValues_CreatesInTerm()
    {
        var term = new FilterTerm("status", ["active", "pending"]);

        Assert.Equal(FilterOperator.In, term.Operator);
        Assert.Equal(["active", "pending"], term.Values);
        Assert.Equal("active", term.Value);
    }

    [Fact]
    public void FilterTerm_WithoutValues_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new FilterTerm("status", Array.Empty<string>()));
    }

    [Fact]
    public void FilterTerm_WithTooManyValues_ThrowsArgumentException()
    {
        var values = Enumerable
            .Repeat("value", FilterTerm.MaximumValues + 1);

        Assert.Throws<ArgumentException>(
            () => new FilterTerm("status", values));
    }

    [Fact]
    public void FilterTerm_WithNullValueElement_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new FilterTerm("status", ["active", null!]));
    }

    [Fact]
    public void FilterTerm_ComparesByFieldOperatorAndValues()
    {
        var left = new FilterTerm("status", ["active", "pending"]);
        var right = new FilterTerm("  status  ", ["active", "pending"]);
        var different = new FilterTerm("status", ["active"]);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.NotEqual(left, different);
    }

    [Fact]
    public void FacetQueryOptions_NormalizesInputAndMaterializesFilters()
    {
        var filters = new List<FilterTerm> { new("status", "active") };
        var options = new FacetQueryOptions(
            "  category  ",
            "  gold  ",
            SortDirection.Descending,
            filters);

        filters.Add(new FilterTerm("tenantId", "tenant"));

        Assert.Equal("category", options.Field);
        Assert.Equal("gold", options.SearchTerm);
        Assert.Equal(SortDirection.Descending, options.Direction);
        Assert.Single(options.Filters);
        Assert.Equal(FacetQueryOptions.DefaultLimit, options.Limit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(FacetQueryOptions.MaximumLimit + 1)]
    public void FacetQueryOptions_WithInvalidLimit_ThrowsArgumentOutOfRangeException(
        int limit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FacetQueryOptions("category", limit: limit));
    }

    [Fact]
    public void FacetQueryOptions_WithNullFilterElement_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new FacetQueryOptions("category", filters: [null!]));
    }
}
