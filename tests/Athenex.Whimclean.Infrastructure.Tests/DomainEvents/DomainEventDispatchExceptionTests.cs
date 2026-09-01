using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Infrastructure.DomainEvents;

namespace Athenex.Whimclean.Infrastructure.Tests.DomainEvents;

public sealed class DomainEventDispatchExceptionTests
{
    [Fact]
    public void Constructor_ExposesUndeliveredEventsAndInnerException()
    {
        IReadOnlyList<IDomainEvent> undeliveredEvents =
            [new TestDomainEvent(), new TestDomainEvent()];
        var innerException = new InvalidOperationException("Handler failed.");

        var exception = new DomainEventDispatchException(
            undeliveredEvents,
            innerException);

        Assert.Same(undeliveredEvents, exception.UndeliveredEvents);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_WithNullArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DomainEventDispatchException(
                null!,
                new InvalidOperationException()));
        Assert.Throws<ArgumentNullException>(() =>
            new DomainEventDispatchException([new TestDomainEvent()], null!));
    }

    private sealed record TestDomainEvent : DomainEvent;
}
