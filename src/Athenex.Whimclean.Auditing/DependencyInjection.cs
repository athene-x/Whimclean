using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Athenex.Whimclean.Auditing;

/// <summary>
/// Service registrations for the audit log.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the audit pipeline: the
    /// <see cref="AuditCommandBehavior{TMessage, TResponse}" /> as a scoped
    /// open-generic pipeline behavior, the scoped
    /// <see cref="AuditChangeCollector" /> and
    /// <see cref="AuditLogRecorder{TUserId, TTenantId}" />, a
    /// <see cref="NullAuditRequestContext" /> unless one is registered,
    /// and <see cref="TimeProvider.System" /> unless one is registered.
    /// Call it before <c>AddUnitOfWorkBehavior</c> so the audit behavior
    /// wraps the unit of work, and register an
    /// <see cref="IAuditLogWriter{TUserId, TTenantId}" /> separately.
    /// </summary>
    /// <typeparam name="TUserId">Type of the local user identifier.</typeparam>
    /// <typeparam name="TTenantId">Type of the tenant identifier.</typeparam>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddAuditing<TUserId, TTenantId>(
        this IServiceCollection services)
        where TUserId : struct
        where TTenantId : struct
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IAuditRequestContext, NullAuditRequestContext>();
        services.TryAddScoped<IAuditChangeCollector, AuditChangeCollector>();
        services.TryAddScoped<IAuditLogRecorder, AuditLogRecorder<TUserId, TTenantId>>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IPipelineBehavior<,>),
            typeof(AuditCommandBehavior<,>)));

        return services;
    }
}
