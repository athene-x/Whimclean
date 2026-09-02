using Athenex.Whimclean.Domain.Results;
using Xunit;

namespace Athenex.Whimclean.Mediator.Tests;

public sealed class UnitOfWorkBehaviorTests
{
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Handle_SuccessfulResult_CommitsOnce()
    {
        var behavior =
            new UnitOfWorkBehavior<TestCommand, Result>(_unitOfWork);

        var response = await behavior.Handle(
            new TestCommand(),
            (_, _) => ValueTask.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(1, _unitOfWork.Commits);
    }

    [Fact]
    public async Task Handle_NonResultResponse_CommitsOnce()
    {
        var id = Guid.NewGuid();
        var behavior =
            new UnitOfWorkBehavior<TestValueCommand, Guid>(_unitOfWork);

        var response = await behavior.Handle(
            new TestValueCommand(),
            (_, _) => ValueTask.FromResult(id),
            CancellationToken.None);

        Assert.Equal(id, response);
        Assert.Equal(1, _unitOfWork.Commits);
    }

    [Fact]
    public async Task Handle_FailedResult_SkipsCommit()
    {
        var behavior =
            new UnitOfWorkBehavior<TestCommand, Result>(_unitOfWork);

        var response = await behavior.Handle(
            new TestCommand(),
            (_, _) => ValueTask.FromResult(
                Result.Failure(Error.Failure("test", "failed"))),
            CancellationToken.None);

        Assert.True(response.IsFailure);
        Assert.Equal(0, _unitOfWork.Commits);
    }

    [Fact]
    public async Task Handle_HandlerThrows_PropagatesWithoutCommit()
    {
        var behavior =
            new UnitOfWorkBehavior<TestCommand, Result>(_unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new TestCommand(),
                (_, _) => throw new InvalidOperationException("boom"),
                CancellationToken.None).AsTask());

        Assert.Equal(0, _unitOfWork.Commits);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToCommit()
    {
        using var source = new CancellationTokenSource();
        var behavior =
            new UnitOfWorkBehavior<TestCommand, Result>(_unitOfWork);

        await behavior.Handle(
            new TestCommand(),
            (_, _) => ValueTask.FromResult(Result.Success()),
            source.Token);

        Assert.Equal(source.Token, _unitOfWork.LastCancellationToken);
    }
}
