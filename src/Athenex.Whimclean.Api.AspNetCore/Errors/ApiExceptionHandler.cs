using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Athenex.Whimclean.Api.AspNetCore.Errors;

/// <summary>
/// Global <see cref="IExceptionHandler"/> that runs the registered
/// <see cref="IApiExceptionMapper"/> chain and writes the resulting
/// <see cref="ApiErrorResponse"/> body, so every unhandled exception
/// produces the API's single error shape.
/// </summary>
/// <param name="exceptionMappers">
/// The mapper chain, probed in registration order; the first match wins.
/// </param>
/// <param name="logger">
/// Logger for the handled exceptions: mapped ones are logged as warnings,
/// unmapped ones as errors.
/// </param>
public sealed class ApiExceptionHandler(
    IEnumerable<IApiExceptionMapper> exceptionMappers,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Writes the mapped status code and error body when a mapper
    /// recognizes the exception; otherwise responds with 500
    /// <c>"Server.UnexpectedError"</c>, hiding the exception details from
    /// the client. A cancellation caused by the client aborting the
    /// request is logged at debug level and produces no body — nobody is
    /// listening, and it is not a server error. Always handles the
    /// exception.
    /// </summary>
    /// <param name="httpContext">The request whose pipeline threw.</param>
    /// <param name="exception">The unhandled exception.</param>
    /// <param name="cancellationToken">
    /// Cancels writing the response body.
    /// </param>
    /// <returns>
    /// Always <see langword="true"/>: no exception propagates past this
    /// handler.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug(
                exception,
                "The request was aborted by the client.");

            return true;
        }

        var isKnownException = false;
        var mapping = default(ApiExceptionMapping);

        foreach (var exceptionMapper in exceptionMappers)
        {
            if (exceptionMapper.TryMap(exception, out mapping))
            {
                isKnownException = true;
                break;
            }
        }

        if (isKnownException)
        {
            logger.LogWarning(
                exception,
                "A request failed with status code {StatusCode}.",
                mapping.StatusCode);
        }
        else
        {
            mapping = new ApiExceptionMapping(
                StatusCodes.Status500InternalServerError,
                new ApiErrorResponse(
                    "Server.UnexpectedError",
                    "An unexpected server error occurred."));

            logger.LogError(exception, "An unhandled API exception occurred.");
        }

        httpContext.Response.StatusCode = mapping.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(
            mapping.Response,
            cancellationToken);

        return true;
    }
}
