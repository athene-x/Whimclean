using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Athenex.Whimclean.Infrastructure;

/// <summary>
/// Service registrations for the in-process domain event pipeline.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="ServiceProviderDomainEventDispatcher" /> as
    /// the scoped <see cref="IDomainEventDispatcher" />. Uses TryAdd
    /// semantics: an already-registered dispatcher is left in place.
    /// </summary>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddDomainEventDispatcher(
        this IServiceCollection services)
    {
        services.TryAddScoped<
            IDomainEventDispatcher,
            ServiceProviderDomainEventDispatcher>();

        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="THandler" /> as a scoped
    /// <see cref="IDomainEventHandler{TEvent}" /> for
    /// <typeparamref name="TEvent" />. Several handlers may be registered
    /// for the same event; each one is invoked when the event is
    /// dispatched.
    /// </summary>
    /// <typeparam name="TEvent">Event the handler reacts to.</typeparam>
    /// <typeparam name="THandler">Handler implementation.</typeparam>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddDomainEventHandler<TEvent, THandler>(
        this IServiceCollection services)
        where TEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        services.AddScoped<IDomainEventHandler<TEvent>, THandler>();

        return services;
    }
}
