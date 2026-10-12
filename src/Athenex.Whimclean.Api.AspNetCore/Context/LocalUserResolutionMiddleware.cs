using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Api.AspNetCore.Context;

/// <summary>
/// Middleware that turns the authenticated principal's subject claim into
/// the local user identifier through
/// <see cref="ILocalUserLookup{TUserId}" /> and records it on the scoped
/// <see cref="RequestCurrentUser{TUserId}" />. A principal without a
/// local user leaves the current user unset, so the write boundary and
/// the application's own checks can answer with a clear error.
/// </summary>
/// <typeparam name="TUserId">Type of the local user identifier.</typeparam>
/// <param name="next">The next middleware in the pipeline.</param>
public sealed class LocalUserResolutionMiddleware<TUserId>(RequestDelegate next)
    where TUserId : notnull
{
    /// <summary>
    /// Resolves the local user for an authenticated request, then always
    /// continues the pipeline.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext httpContext,
        IOptions<ClaimsCurrentUserOptions> options,
        ILocalUserLookup<TUserId> lookup,
        RequestCurrentUser<TUserId> currentUser)
    {
        var principal = httpContext.User;

        if (principal.Identity?.IsAuthenticated == true)
        {
            var subject = principal.FindFirst(options.Value.ClaimType)?.Value;

            if (!string.IsNullOrWhiteSpace(subject))
            {
                var userId = await lookup.FindByExternalSubjectAsync(
                    subject,
                    httpContext.RequestAborted);

                if (userId is not null &&
                    !EqualityComparer<TUserId>.Default.Equals(userId, default))
                {
                    currentUser.SetUserId(userId);
                }
            }
        }

        await next(httpContext);
    }
}
