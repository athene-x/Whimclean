using Athenex.Whimclean.Domain.Entities;
using Athenex.Whimclean.Domain.Events;

namespace Athenex.Whimclean.Domain.Tests.Entities;

public sealed class AggregateRootTests
{
    [Fact]
    public void RaiseDomainEvent_AddsEventsInOrder()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var first = new TestDomainEvent("first");
        var second = new TestDomainEvent("second");

        aggregate.Raise(first);
        aggregate.Raise(second);

        Assert.Equal([first, second], aggregate.DomainEvents);
    }

    [Fact]
    public void RaiseDomainEvent_WithNull_ThrowsArgumentNullException()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        Assert.Throws<ArgumentNullException>(() => aggregate.Raise(null!));
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.Raise(new TestDomainEvent("event"));

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    private sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id)
    {
        public void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed record TestDomainEvent(string Name) : DomainEvent;
}
