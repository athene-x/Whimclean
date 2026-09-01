using Athenex.Whimclean.Domain.Events;

namespace Athenex.Whimclean.Infrastructure.DomainEvents;

/// <summary>
/// In-process reaction to a committed domain event.
/// <see cref="ServiceProviderDomainEventDispatcher" /> resolves handlers
/// from the service provider by the event's runtime type. Handlers run
/// after the save has succeeded; a failure surfaces as a dispatch error,
/// never as a rollback.
/// </summary>
/// <typeparam name="TEvent">Event the handler reacts to.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Reacts to the event. The token may be
    /// <see cref="CancellationToken.None" /> when invoked post-commit,
    /// since events of already-persisted changes must still be delivered.
    /// </summary>
    Task HandleAsync(
        TEvent domainEvent,
        CancellationToken cancellationToken = default);
}
