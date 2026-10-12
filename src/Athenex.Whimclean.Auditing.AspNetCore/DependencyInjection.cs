using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Athenex.Whimclean.Auditing.AspNetCore;

/// <summary>
/// Service registrations for the request-backed audit context.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="HttpAuditRequestContext" /> as the scoped
    /// <see cref="IAuditRequestContext" />, replacing the null context
    /// that <c>AddAuditing</c> registers, and the HTTP context accessor
    /// it needs.
    /// </summary>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddHttpAuditRequestContext(
        this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.Replace(ServiceDescriptor.Scoped<IAuditRequestContext, HttpAuditRequestContext>());

        return services;
    }
}
