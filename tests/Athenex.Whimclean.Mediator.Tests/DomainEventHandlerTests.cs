using Athenex.Whimclean.Domain.Events;
using Xunit;

namespace Athenex.Whimclean.Mediator.Tests;

public sealed class DomainEventHandlerTests
{
    [Fact]
    public async Task Handle_MatchingEvent_ForwardsToHandleAsync()
    {
        var handler = new RecordingTestEventHandler();
        var domainEvent = new TestEvent();

        await handler.Handle(
            new DomainEventNotification(domainEvent),
            CancellationToken.None);

        Assert.Same(domainEvent, Assert.Single(handler.Handled));
    }

    [Fact]
    public async Task Handle_OtherEvent_IsNoOp()
    {
        var handler = new RecordingTestEventHandler();

        await handler.Handle(
            new DomainEventNotification(new OtherTestEvent()),
            CancellationToken.None);

        Assert.Empty(handler.Handled);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationToken()
    {
        using var source = new CancellationTokenSource();
        var handler = new RecordingTestEventHandler();

        await handler.Handle(
            new DomainEventNotification(new TestEvent()),
            source.Token);

        Assert.Equal(source.Token, handler.LastCancellationToken);
    }

    [Fact]
    public async Task Handle_NullNotification_Throws()
    {
        var handler = new RecordingTestEventHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.Handle(null!, CancellationToken.None).AsTask());
    }

    private sealed record OtherTestEvent : DomainEvent;

    private sealed class RecordingTestEventHandler
        : DomainEventHandler<TestEvent>
    {
        public List<TestEvent> Handled { get; } = [];

        public CancellationToken LastCancellationToken { get; private set; }

        protected override ValueTask HandleAsync(
            TestEvent domainEvent,
            CancellationToken cancellationToken)
        {
            Handled.Add(domainEvent);
            LastCancellationToken = cancellationToken;
            return ValueTask.CompletedTask;
        }
    }
}
