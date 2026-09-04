using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Mediator;

namespace Athenex.Whimclean.Mediator;

/// <summary>
/// Dispatches a domain event by wrapping it in a
/// <see cref="DomainEventNotification" /> and publishing it through
/// <see cref="IPublisher" />. Handler execution order and error behavior
/// follow the mediator's configured notification publisher.
/// </summary>
public sealed class MediatorDomainEventDispatcher(
    IPublisher publisher) : IDomainEventDispatcher
{
    /// <summary>
    /// Publishes the event as a <see cref="DomainEventNotification" />;
    /// a no-op when the mediator knows no handler for it.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="domainEvent" /> is <see langword="null" />.
    /// </exception>
    public async Task DispatchAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        try
        {
            await publisher.Publish(
                new DomainEventNotification(domainEvent),
                cancellationToken);
        }
        catch (MissingMessageHandlerException)
        {
            // The dispatcher contract requires a no-op for events without
            // handlers; the source-generated mediator instead throws when
            // no DomainEventHandler<TEvent> exists in its compilation.
        }
    }
}
