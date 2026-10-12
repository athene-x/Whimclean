using Athenex.Whimclean.Api.AspNetCore.Errors;
using FluentValidation.Results;

namespace Athenex.Whimclean.Api.AspNetCore.Tests.Errors;

public sealed class ValidationHttpMapperTests
{
    [Fact]
    public void ToApiErrorResponse_RendersFailuresWithCamelCaseFields()
    {
        var failures = new[]
        {
            new ValidationFailure("SearchTerm", "Too long.") { ErrorCode = "MaximumLengthValidator" },
            new ValidationFailure("Filters[0]", "Bad filter.") { ErrorCode = "CustomValidator" },
            new ValidationFailure(string.Empty, "Whole request.") { ErrorCode = "PredicateValidator" }
        };

        var response = failures.ToApiErrorResponse();

        Assert.Equal(ValidationHttpMapper.ValidationFailedCode, response.Code);
        Assert.NotNull(response.Errors);
        Assert.Collection(
            response.Errors,
            error =>
            {
                Assert.Equal("MaximumLengthValidator", error.Code);
                Assert.Equal("Too long.", error.Description);
                Assert.Equal("searchTerm", error.Field);
            },
            error => Assert.Equal("filters[0]", error.Field),
            error => Assert.Null(error.Field));
    }

    [Fact]
    public void ToHttpResult_RejectsValidResult()
    {
        Assert.Throws<InvalidOperationException>(() => new ValidationResult().ToHttpResult());
    }
}
