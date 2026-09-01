using Athenex.Whimclean.Api.AspNetCore.Errors;
using Athenex.Whimclean.Domain.Results;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.AspNetCore.Tests.Errors;

public sealed class ResultHttpMapperTests
{
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    public void ToHttpResult_MapsErrorTypeAndPayload(
        ErrorType errorType,
        int expectedStatusCode)
    {
        var error = CreateError(errorType);

        var result = error.ToHttpResult();

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        var valueResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        var response = Assert.IsType<ApiErrorResponse>(valueResult.Value);

        Assert.Equal(expectedStatusCode, statusResult.StatusCode);
        Assert.Equal(error.Code, response.Code);
        Assert.Equal(error.Description, response.Description);
    }

    [Fact]
    public void ToHttpResult_WithNoneError_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => Error.None.ToHttpResult());
    }

    [Fact]
    public void ToHttpResult_WithSuccessfulResult_UsesSuccessMapping()
    {
        Result<int> result = 42;

        var httpResult = result.ToHttpResult(value => Results.Ok(value));

        var valueResult = Assert.IsAssignableFrom<IValueHttpResult>(httpResult);
        Assert.Equal(42, valueResult.Value);
    }

    [Fact]
    public void ToHttpResult_WithFailedResult_MapsError()
    {
        Result<int> result = Error.NotFound("Test.NotFound", "Missing.");

        var httpResult = result.ToHttpResult(value => Results.Ok(value));

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }

    private static Error CreateError(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => Error.Validation("Test.Validation", "Validation failed."),
            ErrorType.Unauthorized => Error.Unauthorized("Test.Unauthorized", "Authentication required."),
            ErrorType.Forbidden => Error.Forbidden("Test.Forbidden", "Access denied."),
            ErrorType.NotFound => Error.NotFound("Test.NotFound", "Resource not found."),
            ErrorType.Conflict => Error.Conflict("Test.Conflict", "Resource conflict."),
            _ => Error.Failure("Test.Failure", "Operation failed.")
        };
}
