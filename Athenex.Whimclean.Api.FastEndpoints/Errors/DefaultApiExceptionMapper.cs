using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Application.Persistence;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.FastEndpoints.Errors;

public sealed class DefaultApiExceptionMapper : IApiExceptionMapper
{
    public bool TryMap(
        Exception exception,
        out ApiExceptionMapping mapping)
    {
        mapping = exception switch
        {
            CurrentUserRequiredException => new ApiExceptionMapping(
                StatusCodes.Status401Unauthorized,
                new ApiErrorResponse(
                    "Authentication.CurrentUserRequired",
                    exception.Message)),
            PersistenceConcurrencyException => new ApiExceptionMapping(
                StatusCodes.Status409Conflict,
                new ApiErrorResponse(
                    "Persistence.ConcurrentUpdate",
                    "The record changed after it was loaded. Reload it and try again.")),
            _ => default
        };

        return mapping.Response is not null;
    }
}
