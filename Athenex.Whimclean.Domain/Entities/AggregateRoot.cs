using Athenex.Whimclean.Domain.Events;

namespace Athenex.Whimclean.Domain.Entities;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private IReadOnlyCollection<IDomainEvent>? _domainEventsView;

    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEventsView ??= _domainEvents.AsReadOnly();

    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
