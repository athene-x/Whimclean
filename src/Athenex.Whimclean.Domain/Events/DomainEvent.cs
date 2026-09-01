namespace Athenex.Whimclean.Domain.Events;

/// <summary>
/// Base record for domain events, giving every event a unique
/// <see cref="EventId" /> at creation. Derive concrete events from this
/// type so consumers can deduplicate and handle them idempotently.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Stable identity of this event instance, enabling deduplication and
    /// idempotent handling (e.g. by an outbox or a retried consumer).
    /// Participates in record equality, so two events with identical payloads
    /// remain distinct occurrences.
    /// </summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}
