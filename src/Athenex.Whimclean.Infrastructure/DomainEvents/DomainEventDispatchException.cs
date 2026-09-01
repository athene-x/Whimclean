namespace Athenex.Whimclean.Infrastructure.DomainEvents;

/// <summary>
/// Signals that a commit succeeded but dispatching its domain events did
/// not. The save must NOT be retried — the data is already persisted;
/// instead handle or replay the undelivered events. The failing handler's
/// exception is the <see cref="Exception.InnerException" />.
/// </summary>
/// <param name="innerException">The dispatch failure being wrapped.</param>
public sealed class DomainEventDispatchException(Exception innerException)
    : Exception(
        "The changes were persisted, but dispatching domain events failed. " +
        "Do not retry the save; handle or replay the undelivered events.",
        innerException);
