using Athenex.Whimclean.Application.Persistence;
using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Domain.Results;
using Mediator;

namespace Athenex.Whimclean.Mediator.Tests;

internal sealed record TestEvent : DomainEvent;

internal sealed record TestCommand : ICommand<Result>;

internal sealed record TestValueCommand : ICommand<Guid>;

internal sealed record TestQuery : IQuery<Guid>;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Commits { get; private set; }

    public CancellationToken LastCancellationToken { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Commits++;
        LastCancellationToken = cancellationToken;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingPublisher : IPublisher
{
    public List<object> Published { get; } = [];

    public Exception? ExceptionToThrow { get; set; }

    public ValueTask Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        Published.Add(notification);
        return ValueTask.CompletedTask;
    }

    public ValueTask Publish(
        object notification,
        CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        Published.Add(notification);
        return ValueTask.CompletedTask;
    }
}
