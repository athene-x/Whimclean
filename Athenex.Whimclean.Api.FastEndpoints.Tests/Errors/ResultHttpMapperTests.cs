using Athenex.Whimclean.Api.FastEndpoints.Errors;
using Athenex.Whimclean.Domain.Results;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.FastEndpoints.Tests.Errors;

public sealed class ResultHttpMapperTests
{
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
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

    private static Error CreateError(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => Error.Validation("Test.Validation", "Validation failed."),
            ErrorType.NotFound => Error.NotFound("Test.NotFound", "Resource not found."),
            ErrorType.Conflict => Error.Conflict("Test.Conflict", "Resource conflict."),
            _ => Error.Failure("Test.Failure", "Operation failed.")
        };
}
