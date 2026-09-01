using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Application.Persistence;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.AspNetCore.Errors;

/// <summary>
/// Built-in exception mappings for the framework-neutral application
/// exceptions: <c>CurrentUserRequiredException</c> becomes 401 and
/// <c>PersistenceConcurrencyException</c> becomes 409.
/// </summary>
public sealed class DefaultApiExceptionMapper : IApiExceptionMapper
{
    /// <summary>
    /// Maps <c>CurrentUserRequiredException</c> to 401
    /// <c>"Authentication.CurrentUserRequired"</c> and
    /// <c>PersistenceConcurrencyException</c> to 409
    /// <c>"Persistence.ConcurrentUpdate"</c>; all other exceptions are
    /// left unmapped.
    /// </summary>
    /// <param name="exception">The exception to translate.</param>
    /// <param name="mapping">
    /// The resulting mapping when the exception is recognized; otherwise
    /// the default value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the exception was one of the known types;
    /// otherwise <see langword="false"/>.
    /// </returns>
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
