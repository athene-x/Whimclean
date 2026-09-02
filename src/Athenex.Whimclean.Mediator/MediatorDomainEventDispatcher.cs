using System.Collections.Concurrent;
using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Mediator;

namespace Athenex.Whimclean.Mediator;

/// <summary>
/// Dispatches a domain event by wrapping it in a
/// <see cref="DomainEventNotification{TEvent}" /> closed over the event's
/// runtime type and publishing it through <see cref="IPublisher" />.
/// Handler execution order and error behavior follow the mediator's
/// configured notification publisher.
/// </summary>
public sealed class MediatorDomainEventDispatcher(
    IPublisher publisher) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Type>
        NotificationTypes = new();

    /// <summary>
    /// Publishes the event as a
    /// <see cref="DomainEventNotification{TEvent}" /> for its runtime
    /// type; a no-op when the mediator knows no handler for it.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="domainEvent" /> is <see langword="null" />.
    /// </exception>
    public async Task DispatchAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var notificationType = NotificationTypes.GetOrAdd(
            domainEvent.GetType(),
            static eventType => typeof(DomainEventNotification<>)
                .MakeGenericType(eventType));

        var notification = (INotification)Activator.CreateInstance(
            notificationType,
            domainEvent)!;

        try
        {
            await publisher.Publish(notification, cancellationToken);
        }
        catch (MissingMessageHandlerException)
        {
            // The dispatcher contract requires a no-op for events without
            // handlers; the source-generated mediator instead throws for
            // notification types absent from its compilation.
        }
    }
}
