using Athenex.Whimclean.Infrastructure.DomainEvents;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Athenex.Whimclean.Mediator;

/// <summary>
/// Service registrations for the mediator integration. The consuming
/// application registers the mediator itself (via the source-generated
/// <c>AddMediator</c>); these helpers plug the library seams into it.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="MediatorDomainEventDispatcher" /> as the
    /// scoped <see cref="IDomainEventDispatcher" />, so domain events are
    /// delivered as <see cref="DomainEventNotification" />
    /// notifications. Uses TryAdd semantics: an already-registered
    /// dispatcher is left in place, so call this before any registration
    /// of the default dispatcher.
    /// </summary>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddMediatorDomainEventDispatcher(
        this IServiceCollection services)
    {
        services.TryAddScoped<
            IDomainEventDispatcher,
            MediatorDomainEventDispatcher>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="UnitOfWorkBehavior{TMessage, TResponse}" />
    /// as a scoped open-generic <see cref="IPipelineBehavior{TMessage,
    /// TResponse}" /> for commands. Uses TryAddEnumerable semantics:
    /// registering twice keeps a single registration.
    /// </summary>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddUnitOfWorkBehavior(
        this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IPipelineBehavior<,>),
            typeof(UnitOfWorkBehavior<,>)));

        return services;
    }
}
