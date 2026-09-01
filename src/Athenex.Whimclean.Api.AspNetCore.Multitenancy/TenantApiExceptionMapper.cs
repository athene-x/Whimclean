using Athenex.Whimclean.Api.AspNetCore.Errors;
using Athenex.Whimclean.Multitenancy;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Exception mappings contributed by the multitenancy building block:
/// <c>CurrentTenantRequiredException</c> and
/// <c>TenantAccessViolationException</c> both become 403.
/// </summary>
public sealed class TenantApiExceptionMapper : IApiExceptionMapper
{
    /// <summary>
    /// Maps <c>CurrentTenantRequiredException</c> to 403
    /// <c>"Tenancy.CurrentTenantRequired"</c> and
    /// <c>TenantAccessViolationException</c> to 403
    /// <c>"Tenancy.AccessDenied"</c>; all other exceptions are left
    /// unmapped.
    /// </summary>
    /// <param name="exception">The exception to translate.</param>
    /// <param name="mapping">
    /// The resulting mapping when the exception is recognized; otherwise
    /// the default value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the exception was one of the known
    /// tenancy exceptions; otherwise <see langword="false"/>.
    /// </returns>
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
