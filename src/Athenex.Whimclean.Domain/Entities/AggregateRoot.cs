using Athenex.Whimclean.Domain.Events;

namespace Athenex.Whimclean.Domain.Entities;

/// <summary>
/// Base class for the consistency-boundary entity of an aggregate: the
/// only member of the aggregate that outside code may hold a reference to,
/// and the place where domain events are collected via
/// <see cref="RaiseDomainEvent" /> until the unit of work commits.
/// </summary>
/// <typeparam name="TId">
/// The identifier type; its default value marks a transient (not yet
/// persisted) instance, as defined by <see cref="Entity{TId}" />.
/// </typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private IReadOnlyCollection<IDomainEvent>? _domainEventsView;

    /// <summary>
    /// The events raised on this instance and not yet cleared, in the
    /// order they were raised. A live read-only view: events raised later
    /// appear in a previously obtained reference.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEventsView ??= _domainEvents.AsReadOnly();

    /// <summary>
    /// Initializes the aggregate with an established identity.
    /// </summary>
    /// <param name="id">The identifier; must not be the default value.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="id" /> is the default value of
    /// <typeparamref name="TId" />.
    /// </exception>
    protected AggregateRoot(TId id) : base(id)
    {
    }

    /// <summary>
    /// Initializes a transient aggregate. Exists for ORM materialization;
    /// domain code should use the identity-taking constructor.
    /// </summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Queues an event for dispatch after the unit of work commits.
    /// Raising does not publish anything by itself; the infrastructure
    /// dispatches and then clears the queue via
    /// <see cref="IHasDomainEvents" />.
    /// </summary>
    /// <param name="domainEvent">The event to queue; must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="domainEvent" /> is <see langword="null" />.
    /// </exception>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    // Explicit implementation: clearing dispatched events is an
    // infrastructure concern, so it is reachable only through
    // IHasDomainEvents and stays off the aggregate's domain surface.
    void IHasDomainEvents.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
