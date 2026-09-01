using Athenex.Whimclean.Domain.Results;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.AspNetCore.Errors;

/// <summary>
/// Maps domain <see cref="Error"/> and <see cref="Result"/> values to
/// minimal-API <see cref="IResult"/> responses carrying the shared
/// <see cref="ApiErrorResponse"/> body.
/// </summary>
public static class ResultHttpMapper
{
    /// <summary>
    /// Produces a JSON failure response for this error. The status code
    /// follows <see cref="ErrorType"/>: Validation 400, Unauthorized 401,
    /// Forbidden 403, NotFound 404, Conflict 409, anything else 500.
    /// </summary>
    /// <param name="error">The failure to translate.</param>
    /// <returns>
    /// A JSON <see cref="IResult"/> whose body is an
    /// <see cref="ApiErrorResponse"/> built from the error's code and
    /// description.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="error"/> is <see cref="Error.None"/>; a success
    /// cannot be mapped to an error response.
    /// </exception>
    public static IResult ToHttpResult(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (error.Type == ErrorType.None)
        {
            throw new InvalidOperationException(
                "A success cannot be mapped to an error response.");
        }

        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Json(
            new ApiErrorResponse(error.Code, error.Description),
            statusCode: statusCode);
    }

    /// <summary>
    /// Converts a <see cref="Result"/> to an HTTP response: invokes
    /// <paramref name="onSuccess"/> when the result succeeded, otherwise
    /// maps the error via <see cref="ToHttpResult(Error)"/>.
    /// </summary>
    /// <param name="result">The operation outcome to translate.</param>
    /// <param name="onSuccess">
    /// Factory producing the success response.
    /// </param>
    /// <returns>
    /// The success response, or the mapped failure response.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <see langword="null"/>.
    /// </exception>
    public static IResult ToHttpResult(
        this Result result,
        Func<IResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Match(onSuccess, error => error.ToHttpResult());
    }

    /// <summary>
    /// Converts a <see cref="Result{TValue}"/> to an HTTP response:
    /// invokes <paramref name="onSuccess"/> with the value when the
    /// result succeeded, otherwise maps the error via
    /// <see cref="ToHttpResult(Error)"/>.
    /// </summary>
    /// <typeparam name="TValue">Type of the success value.</typeparam>
    /// <param name="result">The operation outcome to translate.</param>
    /// <param name="onSuccess">
    /// Factory producing the success response from the value.
    /// </param>
    /// <returns>
    /// The success response, or the mapped failure response.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <see langword="null"/>.
    /// </exception>
    public static IResult ToHttpResult<TValue>(
        this Result<TValue> result,
        Func<TValue, IResult> onSuccess)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Match(onSuccess, error => error.ToHttpResult());
    }
}
