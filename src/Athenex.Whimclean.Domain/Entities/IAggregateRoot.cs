using Athenex.Whimclean.Domain.Events;

namespace Athenex.Whimclean.Domain.Entities;

/// <summary>
/// Marks the consistency-boundary entity of an aggregate: the only member
/// of the aggregate that repositories load and save, and the collection
/// point for its domain events (via <see cref="IHasDomainEvents" />).
/// </summary>
/// <typeparam name="TId">The aggregate's identifier type.</typeparam>
public interface IAggregateRoot<out TId> : IEntity<TId>, IHasDomainEvents
    where TId : notnull
{
}
