namespace Athenex.Whimclean.Api.AspNetCore.Errors;

/// <summary>
/// Translates known exceptions into HTTP error responses. Implementations
/// are registered via <c>TryAddEnumerable</c>, so multiple packages can
/// contribute mappings without overriding each other; the first mapper
/// that recognizes an exception wins.
/// </summary>
public interface IApiExceptionMapper
{
    /// <summary>
    /// Attempts to map <paramref name="exception"/> to an HTTP status code
    /// and <see cref="ApiErrorResponse"/> body.
    /// </summary>
    /// <param name="exception">The exception to translate.</param>
    /// <param name="mapping">
    /// The resulting mapping when the exception is recognized; otherwise
    /// the default value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if this mapper recognized the exception;
    /// otherwise <see langword="false"/>, letting the next mapper try.
    /// </returns>
    bool TryMap(
        Exception exception,
        out ApiExceptionMapping mapping);
}

/// <summary>
/// The outcome of a successful exception mapping: the HTTP status code to
/// return together with the <see cref="ApiErrorResponse"/> body.
/// </summary>
/// <param name="StatusCode">HTTP status code of the failure response.</param>
/// <param name="Response">Error body written to the response.</param>
public readonly record struct ApiExceptionMapping(
    int StatusCode,
    ApiErrorResponse Response);
