using System.Diagnostics.CodeAnalysis;
using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Domain.Results;
using Mediator;

namespace Athenex.Whimclean.Auditing.Tests;

internal sealed record CreateThingCommand(string Name) : ICommand<Result<Guid>>, IAuditedCommand
{
    public AuditDescriptor Describe() => new("Thing.Create", "Thing", EntityReference: Name);
}

internal sealed record RenameThingCommand(Guid Id) : ICommand<Result>, IAuditedCommand
{
    public AuditDescriptor Describe() => new("Thing.Rename", "Thing", Id.ToString(), TenantId: Guid.Parse("4a7d1ed4-14ee-4f8a-9c16-0b1c1f0f0001"), Reason: "typo");
}

internal sealed record DescribeThingCommand : ICommand<Result<ThingDto>>, IAuditedCommand
{
    public AuditDescriptor Describe() => new("Thing.Describe", "Thing");
}

internal sealed record ThingDto(string Name);

internal sealed class RecordingRecorder : IAuditLogRecorder
{
    public List<(AuditDescriptor Descriptor, AuditResult Result, string? ErrorCode, string? CreatedId)> Records { get; } = [];

    public ValueTask RecordAsync(
        AuditDescriptor descriptor,
        AuditResult result,
        string? errorCode,
        string? createdId,
        CancellationToken cancellationToken = default)
    {
        Records.Add((descriptor, result, errorCode, createdId));
        return ValueTask.CompletedTask;
    }
}

internal sealed class RecordingWriter : IAuditLogWriter<Guid, Guid>
{
    public List<AuditLog<Guid, Guid>> Entries { get; } = [];

    public ValueTask WriteAsync(AuditLog<Guid, Guid> entry, CancellationToken cancellationToken = default)
    {
        Entries.Add(entry);
        return ValueTask.CompletedTask;
    }
}

internal sealed class FixedCurrentUser(Guid? userId) : ICurrentUser<Guid>
{
    public bool TryGetUserId([MaybeNullWhen(false)] out Guid id)
    {
        id = userId.GetValueOrDefault();
        return userId.HasValue;
    }
}

internal sealed class FixedRequestContext(
    string? actorName = null,
    string? correlationId = null,
    string? ipAddress = null,
    string? userAgent = null) : IAuditRequestContext
{
    public string? ActorName => actorName;
    public string? CorrelationId => correlationId;
    public string? IpAddress => ipAddress;
    public string? UserAgent => userAgent;
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
