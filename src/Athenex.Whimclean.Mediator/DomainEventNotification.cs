using Athenex.Whimclean.Domain.Events;
using Mediator;

namespace Athenex.Whimclean.Mediator;

/// <summary>
/// Adapts a domain event to a mediator <see cref="INotification" />.
/// Domain events cannot implement <see cref="INotification" /> themselves
/// because the domain layer takes no mediator dependency, so the
/// dispatcher wraps each event in this notification instead.
/// </summary>
/// <remarks>
/// The wrapper is deliberately non-generic. The Mediator source generator
/// does not support generic messages: an open generic
/// <c>INotification</c> anywhere in the compilation or its references
/// makes the generated code fail to compile. Handlers therefore derive
/// from <see cref="DomainEventHandler{TEvent}" />, which filters on the
/// event's runtime type, instead of subscribing to a closed generic
/// notification.
/// </remarks>
/// <param name="DomainEvent">The domain event being delivered.</param>
public sealed record DomainEventNotification(IDomainEvent DomainEvent)
    : INotification;
