using Microsoft.AspNetCore.Builder;

namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Pipeline configuration for the ASP.NET Core multitenancy building
/// block.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="TenantResolutionMiddleware{TTenantId}"/>, which
    /// records the tenant each request claims to target without
    /// authorizing it. Place after <c>UseAuthentication</c> and before
    /// <c>UseAuthorization</c>, so the requested tenant is available when
    /// the tenant-member policy is evaluated.
    /// </summary>
    /// <typeparam name="TTenantId">Type of the tenant identifier.</typeparam>
    /// <param name="app">The application pipeline to configure.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IApplicationBuilder UseTenantResolution<TTenantId>(
        this IApplicationBuilder app)
        where TTenantId : notnull =>
        app.UseMiddleware<TenantResolutionMiddleware<TTenantId>>();
}
