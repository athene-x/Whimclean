using Mediator;
using Xunit;

namespace Athenex.Whimclean.Mediator.Tests;

public sealed class MediatorDomainEventDispatcherTests
{
    private readonly RecordingPublisher _publisher = new();

    [Fact]
    public async Task DispatchAsync_WrapsEventInTypedNotification()
    {
        var dispatcher = new MediatorDomainEventDispatcher(_publisher);
        var domainEvent = new TestEvent();

        await dispatcher.DispatchAsync(domainEvent);

        var notification = Assert.IsType<DomainEventNotification<TestEvent>>(
            Assert.Single(_publisher.Published));
        Assert.Same(domainEvent, notification.DomainEvent);
    }

    [Fact]
    public async Task DispatchAsync_NullEvent_Throws()
    {
        var dispatcher = new MediatorDomainEventDispatcher(_publisher);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.DispatchAsync(null!));
    }

    [Fact]
    public async Task DispatchAsync_MissingHandler_IsNoOp()
    {
        _publisher.ExceptionToThrow =
            new MissingMessageHandlerException(new TestEvent());
        var dispatcher = new MediatorDomainEventDispatcher(_publisher);

        await dispatcher.DispatchAsync(new TestEvent());
    }

    [Fact]
    public async Task DispatchAsync_HandlerFailure_Propagates()
    {
        _publisher.ExceptionToThrow =
            new InvalidOperationException("handler failed");
        var dispatcher = new MediatorDomainEventDispatcher(_publisher);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(new TestEvent()));
    }
}
