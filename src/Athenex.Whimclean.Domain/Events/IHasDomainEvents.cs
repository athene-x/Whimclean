namespace Athenex.Whimclean.Domain.Events;

/// <summary>
/// The infrastructure-facing surface for collecting domain events: the
/// dispatcher reads <see cref="DomainEvents" /> after the unit of work
/// commits and then calls <see cref="ClearDomainEvents" />. Domain code
/// only raises events; it never clears them.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// The events raised on this instance and not yet cleared, in the
    /// order they were raised.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Empties <see cref="DomainEvents" /> after the pending events have
    /// been dispatched. Intended for the infrastructure only; calling it
    /// early discards undispatched events.
    /// </summary>
    void ClearDomainEvents();
}
