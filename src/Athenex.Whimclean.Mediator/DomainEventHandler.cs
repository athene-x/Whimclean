using Athenex.Whimclean.Domain.Events;
using Mediator;

namespace Athenex.Whimclean.Mediator;

/// <summary>
/// Base class for a mediator notification handler that reacts to one
/// domain event type. Every <see cref="DomainEventNotification" /> is
/// delivered to every registered handler; this base performs the type
/// filter so <see cref="HandleAsync" /> runs only for
/// <typeparamref name="TEvent" /> and returns synchronously otherwise.
/// </summary>
/// <typeparam name="TEvent">Domain event the handler reacts to.</typeparam>
public abstract class DomainEventHandler<TEvent>
    : INotificationHandler<DomainEventNotification>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Filters the notification on <typeparamref name="TEvent" /> and
    /// forwards matching events to <see cref="HandleAsync" />.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="notification" /> is <see langword="null" />.
    /// </exception>
    public ValueTask Handle(
        DomainEventNotification notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return notification.DomainEvent is TEvent domainEvent
            ? HandleAsync(domainEvent, cancellationToken)
            : ValueTask.CompletedTask;
    }

    /// <summary>
    /// Reacts to the event. Runs after the unit of work has committed;
    /// the token may be <see cref="CancellationToken.None" /> because
    /// events of persisted changes must still be delivered.
    /// </summary>
    protected abstract ValueTask HandleAsync(
        TEvent domainEvent,
        CancellationToken cancellationToken);
}
