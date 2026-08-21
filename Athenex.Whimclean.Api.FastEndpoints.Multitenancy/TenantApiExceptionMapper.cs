using Athenex.Whimclean.Api.FastEndpoints.Errors;
using Athenex.Whimclean.Multitenancy;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.FastEndpoints.Multitenancy;

public sealed class TenantApiExceptionMapper : IApiExceptionMapper
{
    public bool TryMap(
        Exception exception,
        out ApiExceptionMapping mapping)
    {
        mapping = exception switch
        {
            CurrentTenantRequiredException => new ApiExceptionMapping(
                StatusCodes.Status403Forbidden,
                new ApiErrorResponse(
                    "Tenancy.CurrentTenantRequired",
                    exception.Message)),
            TenantAccessViolationException => new ApiExceptionMapping(
                StatusCodes.Status403Forbidden,
                new ApiErrorResponse(
                    "Tenancy.AccessDenied",
                    exception.Message)),
            _ => default
        };

        return mapping.Response is not null;
    }
}
