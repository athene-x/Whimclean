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
    public void FilterTerm_NormalizesFieldAndValue()
    {
        var term = new FilterTerm(
            "  status  ",
            "  active  ",
            FilterOperator.Equal);

        Assert.Equal("status", term.Field);
        Assert.Equal("active", term.Value);
        Assert.Equal(FilterOperator.Equal, term.Operator);
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
    }
}
