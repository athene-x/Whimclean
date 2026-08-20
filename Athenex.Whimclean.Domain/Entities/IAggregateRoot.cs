using Athenex.Whimclean.Domain.Events;

namespace Athenex.Whimclean.Domain.Entities;

public interface IAggregateRoot<out TId> : IEntity<TId>, IHasDomainEvents
    where TId : notnull
{
}
