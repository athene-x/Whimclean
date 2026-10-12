using Athenex.Whimclean.Api.AspNetCore.Queries;
using Athenex.Whimclean.Application.Queries;

namespace Athenex.Whimclean.Api.AspNetCore.Tests.Queries;

public sealed class ListQueryRequestValidatorTests
{
    private enum Status
    {
        Active,
        Suspended
    }

    private sealed class ListThingsRequest : ListQueryRequest;

    private sealed class OpenValidator : ListQueryRequestValidator<ListThingsRequest>;

    private sealed class RestrictedValidator : ListQueryRequestValidator<ListThingsRequest>
    {
        public RestrictedValidator()
        {
            AllowSortFields("name", "createdAt");
            AllowEnumFilterField<Status>("status");
            AllowFilterField("createdAt", FilterOperator.GreaterThanOrEqual, FilterOperator.LessThan);
        }
    }

    [Fact]
    public void OpenValidator_AcceptsWellFormedInput()
    {
        var result = new OpenValidator().Validate(new ListThingsRequest
        {
            SearchTerm = "x",
            Sort = "anything,-else",
            Filters = ["field:cn:value", "other:in:a,b"],
            Page = 1,
            Size = ListQueryOptions.MaximumSize
        });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("status:xx:value", "unknown operator")]
    [InlineData("status", "pattern")]
    public void OpenValidator_RejectsMalformedFilters(string filter, string expected)
    {
        var result = new OpenValidator().Validate(new ListThingsRequest { Filters = [filter] });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains(expected));
    }

    [Fact]
    public void OpenValidator_RejectsPageAndSizeOutOfRange()
    {
        var result = new OpenValidator().Validate(new ListThingsRequest
        {
            Page = 0,
            Size = ListQueryOptions.MaximumSize + 1
        });

        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void RestrictedValidator_AcceptsAllowedFields()
    {
        var result = new RestrictedValidator().Validate(new ListThingsRequest
        {
            Sort = "-createdAt,name",
            Filters = ["status:in:active,SUSPENDED", "createdAt:ge:2026-01-01"]
        });

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(error => error.ErrorMessage)));
    }

    [Theory]
    [InlineData("email", null, "Sort field 'email' is not supported.")]
    [InlineData(null, "email:eq:x", "Filter field 'email' is not supported.")]
    [InlineData(null, "status:cn:act", "Filter operator 'Contains' is not supported for field 'status'.")]
    [InlineData(null, "status:eq:deleted", "Filter value 'deleted' is not valid for field 'status'.")]
    public void RestrictedValidator_RejectsUnknownFieldsOperatorsAndValues(
        string? sort,
        string? filter,
        string expected)
    {
        var result = new RestrictedValidator().Validate(new ListThingsRequest
        {
            Sort = sort,
            Filters = filter is null ? [] : [filter]
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == expected);
    }

    [Fact]
    public void ToListQueryOptions_ParsesValidatedRequest()
    {
        var options = new ListThingsRequest
        {
            Sort = "-name",
            Filters = ["status:eq:active"],
            Page = 2,
            Size = 5
        }.ToListQueryOptions();

        Assert.Equal(5, options.Offset);
        Assert.Single(options.Sort);
        Assert.Single(options.Filters);
    }
}
