namespace Athenex.Whimclean.Api.FastEndpoints.Errors;

public interface IApiExceptionMapper
{
    bool TryMap(
        Exception exception,
        out ApiExceptionMapping mapping);
}

public readonly record struct ApiExceptionMapping(
    int StatusCode,
    ApiErrorResponse Response);
