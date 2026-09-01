using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Infrastructure.Auditing;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Athenex.Whimclean.Infrastructure;

/// <summary>
/// Service registrations for the in-process domain event pipeline and the
/// built-in audit actor resolvers.
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
    /// dispatched. Uses TryAddEnumerable semantics: registering the same
    /// event/handler pair twice keeps a single registration.
    /// </summary>
    /// <typeparam name="TEvent">Event the handler reacts to.</typeparam>
    /// <typeparam name="THandler">Handler implementation.</typeparam>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddDomainEventHandler<TEvent, THandler>(
        this IServiceCollection services)
        where TEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IDomainEventHandler<TEvent>, THandler>());

        return services;
    }

    /// <summary>
    /// Registers <see cref="IdentityAuditActorResolver{TActor}" /> as the
    /// singleton
    /// <see cref="IAuditActorResolver{TUserId, TActor}" /> for schemas
    /// whose actor column type equals the user id type. Uses TryAdd
    /// semantics: an already-registered resolver is left in place.
    /// </summary>
    /// <typeparam name="TActor">
    /// Type serving as both the user id and the stored actor value.
    /// </typeparam>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddIdentityAuditActorResolver<TActor>(
        this IServiceCollection services)
        where TActor : notnull
    {
        services.TryAddSingleton<
            IAuditActorResolver<TActor, TActor>,
            IdentityAuditActorResolver<TActor>>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="NullableAuditActorResolver{TUserId}" /> as the
    /// singleton
    /// <see cref="IAuditActorResolver{TUserId, TActor}" /> for schemas
    /// whose actor columns are declared as the nullable form of the user
    /// id type. Uses TryAdd semantics: an already-registered resolver is
    /// left in place.
    /// </summary>
    /// <typeparam name="TUserId">Value-type user id being lifted.</typeparam>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddNullableAuditActorResolver<TUserId>(
        this IServiceCollection services)
        where TUserId : struct
    {
        services.TryAddSingleton<
            IAuditActorResolver<TUserId, TUserId?>,
            NullableAuditActorResolver<TUserId>>();

        return services;
    }
}
