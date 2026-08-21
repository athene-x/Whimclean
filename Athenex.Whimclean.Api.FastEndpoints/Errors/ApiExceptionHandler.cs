using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Athenex.Whimclean.Api.FastEndpoints.Errors;

public sealed class ApiExceptionHandler(
    IEnumerable<IApiExceptionMapper> exceptionMappers,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
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

        if (!isKnownException)
        {
            mapping = new ApiExceptionMapping(
                StatusCodes.Status500InternalServerError,
                new ApiErrorResponse(
                    "Server.UnexpectedError",
                    "An unexpected server error occurred."));
        }

        if (!isKnownException)
        {
            logger.LogError(exception, "An unhandled API exception occurred.");
        }
        else
        {
            logger.LogWarning(
                exception,
                "A request failed with status code {StatusCode}.",
                mapping.StatusCode);
        }

        httpContext.Response.StatusCode = mapping.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(
            mapping.Response,
            cancellationToken);

        return true;
    }
}
