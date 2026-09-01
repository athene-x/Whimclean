using System.Collections.Concurrent;
using Athenex.Whimclean.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Infrastructure.DomainEvents;

/// <summary>
/// Dispatches a domain event to every registered
/// <see cref="IDomainEventHandler{TEvent}" /> for the event's runtime type,
/// resolved from the service provider.
/// </summary>
public sealed class ServiceProviderDomainEventDispatcher(
    IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, DispatchInvoker>
        Invokers = new();

    /// <summary>
    /// Resolves every <see cref="IDomainEventHandler{TEvent}" />
    /// registered for the event's runtime type and invokes them
    /// sequentially; a no-op when no handler is registered.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="domainEvent" /> is <see langword="null" />.
    /// </exception>
    public Task DispatchAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var invoker = Invokers.GetOrAdd(
            domainEvent.GetType(),
            static eventType => (DispatchInvoker)Activator.CreateInstance(
                typeof(DispatchInvoker<>).MakeGenericType(eventType))!);

        return invoker.InvokeAsync(
            serviceProvider,
            domainEvent,
            cancellationToken);
    }

    private abstract class DispatchInvoker
    {
        public abstract Task InvokeAsync(
            IServiceProvider serviceProvider,
            IDomainEvent domainEvent,
            CancellationToken cancellationToken);
    }

    private sealed class DispatchInvoker<TEvent> : DispatchInvoker
        where TEvent : IDomainEvent
    {
        public override async Task InvokeAsync(
            IServiceProvider serviceProvider,
            IDomainEvent domainEvent,
            CancellationToken cancellationToken)
        {
            foreach (var handler in serviceProvider
                .GetServices<IDomainEventHandler<TEvent>>())
            {
                await handler.HandleAsync(
                    (TEvent)domainEvent,
                    cancellationToken);
            }
        }
    }
}
