using Athenex.Whimclean.Domain.Events;

namespace Athenex.Whimclean.Infrastructure.DomainEvents;

/// <summary>
/// Signals that a commit succeeded but dispatching its domain events did
/// not. The save must NOT be retried — the data is already persisted;
/// instead handle or replay <see cref="UndeliveredEvents" />. The failing
/// handler's exception is the <see cref="Exception.InnerException" />.
/// </summary>
public sealed class DomainEventDispatchException : Exception
{
    /// <summary>
    /// Creates the exception for the events that never reached their
    /// handlers.
    /// </summary>
    /// <param name="undeliveredEvents">
    /// The events still owed to their handlers: the one whose dispatch
    /// failed first, followed by any that were never attempted.
    /// </param>
    /// <param name="innerException">The dispatch failure being wrapped.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="undeliveredEvents" /> or
    /// <paramref name="innerException" /> is <see langword="null" />.
    /// </exception>
    public DomainEventDispatchException(
        IReadOnlyList<IDomainEvent> undeliveredEvents,
        Exception innerException)
        : base(
            "The changes were persisted, but dispatching domain events failed. " +
            "Do not retry the save; handle or replay the undelivered events.",
            innerException)
    {
        ArgumentNullException.ThrowIfNull(undeliveredEvents);
        ArgumentNullException.ThrowIfNull(innerException);

        UndeliveredEvents = undeliveredEvents;
    }

    /// <summary>
    /// The events that never reached their handlers: the event whose
    /// dispatch failed, followed by any that were never attempted. The
    /// aggregates have already been cleared by the save pipeline, so this
    /// list is the only remaining copy.
    /// </summary>
    public IReadOnlyList<IDomainEvent> UndeliveredEvents { get; }
}
