using Athenex.Whimclean.Domain.Events;

namespace Athenex.Whimclean.Infrastructure.DomainEvents;

/// <summary>
/// Delivers domain events to their in-process handlers. The unit of work
/// invokes it after a successful commit, so delivery is at-most-once: a
/// handler failure never rolls the persisted changes back.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches one event to every handler registered for its runtime
    /// type; a no-op when no handler is registered. Completes when all
    /// handlers have run.
    /// </summary>
    Task DispatchAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default);
}
