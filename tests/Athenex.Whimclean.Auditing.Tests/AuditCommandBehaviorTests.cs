using Athenex.Whimclean.Domain.Results;

namespace Athenex.Whimclean.Auditing.Tests;

public sealed class AuditCommandBehaviorTests
{
    [Fact]
    public async Task Handle_RecordsSuccessWithTheCreatedId()
    {
        var recorder = new RecordingRecorder();
        var behavior = new AuditCommandBehavior<CreateThingCommand, Result<Guid>>(recorder);
        var createdId = Guid.NewGuid();

        var response = await behavior.Handle(
            new CreateThingCommand("thing"),
            (_, _) => ValueTask.FromResult(Result<Guid>.Success(createdId)),
            CancellationToken.None);

        Assert.Equal(createdId, response.Value);
        var record = Assert.Single(recorder.Records);
        Assert.Equal(AuditResult.Success, record.Result);
        Assert.Null(record.ErrorCode);
        Assert.Equal(createdId.ToString(), record.CreatedId);
        Assert.Equal("Thing.Create", record.Descriptor.Action);
    }

    [Fact]
    public async Task Handle_RecordsAFailedResultWithItsErrorCode()
    {
        var recorder = new RecordingRecorder();
        var behavior = new AuditCommandBehavior<RenameThingCommand, Result>(recorder);

        var response = await behavior.Handle(
            new RenameThingCommand(Guid.NewGuid()),
            (_, _) => ValueTask.FromResult(Result.Failure(Error.NotFound("Thing.NotFound", "gone"))),
            CancellationToken.None);

        Assert.True(response.IsFailure);
        var record = Assert.Single(recorder.Records);
        Assert.Equal(AuditResult.Failed, record.Result);
        Assert.Equal("Thing.NotFound", record.ErrorCode);
        Assert.Null(record.CreatedId);
    }

    [Fact]
    public async Task Handle_RecordsAnExceptionAndRethrows()
    {
        var recorder = new RecordingRecorder();
        var behavior = new AuditCommandBehavior<RenameThingCommand, Result>(recorder);

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new RenameThingCommand(Guid.NewGuid()),
            (_, _) => throw new InvalidOperationException("boom"),
            CancellationToken.None).AsTask());

        var record = Assert.Single(recorder.Records);
        Assert.Equal(AuditResult.Failed, record.Result);
        Assert.Equal(nameof(InvalidOperationException), record.ErrorCode);
    }

    [Fact]
    public async Task Handle_IgnoresComplexResultValuesAsCreatedIds()
    {
        var recorder = new RecordingRecorder();
        var behavior = new AuditCommandBehavior<DescribeThingCommand, Result<ThingDto>>(recorder);

        await behavior.Handle(
            new DescribeThingCommand(),
            (_, _) => ValueTask.FromResult(Result<ThingDto>.Success(new ThingDto("x"))),
            CancellationToken.None);

        Assert.Null(Assert.Single(recorder.Records).CreatedId);
    }
}
