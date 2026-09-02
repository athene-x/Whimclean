using Athenex.Whimclean.Domain.Events;
using Mediator;

namespace Athenex.Whimclean.Mediator;

/// <summary>
/// Adapts a domain event to a mediator <see cref="INotification" />.
/// Domain events cannot implement <see cref="INotification" /> themselves
/// because the domain layer takes no mediator dependency, so handlers
/// subscribe to the wrapper instead:
/// <c>INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;</c>.
/// </summary>
/// <typeparam name="TEvent">Concrete domain event being delivered.</typeparam>
/// <param name="DomainEvent">The domain event being delivered.</param>
public sealed record DomainEventNotification<TEvent>(TEvent DomainEvent)
    : INotification
    where TEvent : IDomainEvent;
