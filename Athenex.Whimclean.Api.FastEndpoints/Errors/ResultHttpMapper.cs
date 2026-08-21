using Athenex.Whimclean.Domain.Results;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.FastEndpoints.Errors;

public static class ResultHttpMapper
{
    public static IResult ToHttpResult(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Json(
            new ApiErrorResponse(error.Code, error.Description),
            statusCode: statusCode);
    }
}
