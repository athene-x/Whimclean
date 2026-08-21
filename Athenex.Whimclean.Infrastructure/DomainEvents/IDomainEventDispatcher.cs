using Athenex.Whimclean.Domain.Events;

namespace Athenex.Whimclean.Infrastructure.DomainEvents;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default);
}
