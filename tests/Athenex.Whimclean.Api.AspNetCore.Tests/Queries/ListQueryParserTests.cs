using Athenex.Whimclean.Api.AspNetCore.Queries;
using Athenex.Whimclean.Application.Queries;

namespace Athenex.Whimclean.Api.AspNetCore.Tests.Queries;

public sealed class ListQueryParserTests
{
    [Fact]
    public void Parse_BuildsOptionsFromRawValues()
    {
        var options = ListQueryParser.Parse(
            " order ",
            "name,-createdAt",
            ["status:eq:active", "type:in:a,b"],
            page: 2,
            size: 20);

        Assert.Equal("order", options.SearchTerm);
        Assert.Equal(2, options.Page);
        Assert.Equal(20, options.Size);
        Assert.Collection(
            options.Sort,
            term => Assert.Equal(("name", SortDirection.Ascending), (term.Field, term.Direction)),
            term => Assert.Equal(("createdAt", SortDirection.Descending), (term.Field, term.Direction)));
        Assert.Collection(
            options.Filters,
            term => Assert.Equal(new FilterTerm("status", "active"), term),
            term => Assert.Equal(new FilterTerm("type", ["a", "b"]), term));
    }

    [Theory]
    [InlineData("eq", FilterOperator.Equal)]
    [InlineData("NE", FilterOperator.NotEqual)]
    [InlineData("gt", FilterOperator.GreaterThan)]
    [InlineData("ge", FilterOperator.GreaterThanOrEqual)]
    [InlineData("lt", FilterOperator.LessThan)]
    [InlineData("le", FilterOperator.LessThanOrEqual)]
    [InlineData("cn", FilterOperator.Contains)]
    [InlineData("sw", FilterOperator.StartsWith)]
    [InlineData("ew", FilterOperator.EndsWith)]
    public void ParseFilter_MapsOperatorCodes(string code, FilterOperator expected)
    {
        var term = ListQueryParser.ParseFilter($"field:{code}:value");

        Assert.Equal(expected, term.Operator);
        Assert.Equal("value", term.Value);
    }

    [Fact]
    public void ParseFilter_KeepsColonsInsideValue()
    {
        var term = ListQueryParser.ParseFilter("occurredAt:ge:2026-01-01T10:00:00Z");

        Assert.Equal("2026-01-01T10:00:00Z", term.Value);
    }

    [Theory]
    [InlineData("", "cannot be empty")]
    [InlineData("status", "pattern")]
    [InlineData(":eq:value", "empty field")]
    [InlineData("status:xx:value", "unknown operator")]
    [InlineData("status:eq:", "empty value")]
    [InlineData("status:in:a,,b", "non-empty value")]
    public void TryParseFilter_ReportsMalformedInput(string filter, string expectedError)
    {
        var parsed = ListQueryParser.TryParseFilter(filter, out var term, out var error);

        Assert.False(parsed);
        Assert.Null(term);
        Assert.Contains(expectedError, error);
    }

    [Fact]
    public void ParseFilters_RejectsTooManyFilters()
    {
        var filters = Enumerable
            .Range(0, ListQueryOptions.MaximumFilters + 1)
            .Select(index => $"f{index}:eq:v");

        Assert.Throws<FormatException>(() => ListQueryParser.ParseFilters(filters));
    }

    [Fact]
    public void ParseSort_BlankYieldsEmpty()
    {
        Assert.Empty(ListQueryParser.ParseSort("  "));
        Assert.Empty(ListQueryParser.ParseSort(null));
    }

    [Theory]
    [InlineData("name,", "empty field")]
    [InlineData("-", "empty field")]
    [InlineData("a,b,c,d,e,f", "At most")]
    public void TryParseSort_ReportsMalformedInput(string sort, string expectedError)
    {
        var parsed = ListQueryParser.TryParseSort(sort, out _, out var error);

        Assert.False(parsed);
        Assert.Contains(expectedError, error);
    }
}
