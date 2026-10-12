namespace Athenex.Whimclean.Auditing.Tests;

public sealed class AuditLogRecorderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 9, 30, 0, TimeSpan.FromHours(7));

    private static AuditLogRecorder<Guid, Guid> CreateRecorder(
        Guid? userId,
        IAuditRequestContext request,
        AuditChangeCollector collector,
        RecordingWriter writer) =>
        new(new FixedCurrentUser(userId), request, collector, writer, new FixedTimeProvider(Now));

    [Fact]
    public async Task RecordAsync_AttributesToTheCurrentUserAndMergesCollectedChanges()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var writer = new RecordingWriter();
        var collector = new AuditChangeCollector();
        collector.Add("name", "old", "new");
        collector.SetEntityReference("acme");
        collector.SetTenantId(tenantId);
        var recorder = CreateRecorder(
            userId,
            new FixedRequestContext("Alice", "corr-1", "10.0.0.1", "agent/1"),
            collector,
            writer);

        await recorder.RecordAsync(
            new AuditDescriptor("Tenant.Rename", "Tenant", Metadata: new Dictionary<string, object?> { ["k"] = 1 }),
            AuditResult.Success,
            errorCode: null,
            createdId: "created-id");

        var entry = Assert.Single(writer.Entries);
        Assert.Equal(Now.ToUniversalTime(), entry.OccurredAt);
        Assert.Equal(TimeSpan.Zero, entry.OccurredAt.Offset);
        Assert.Equal(userId, entry.ActorId);
        Assert.Equal("Alice", entry.ActorName);
        Assert.Equal(AuditActorType.User, entry.ActorType);
        Assert.Equal("created-id", entry.EntityId);
        Assert.Equal("acme", entry.EntityReference);
        Assert.Equal(tenantId, entry.TenantId);
        Assert.Equal("""[{"field":"name","oldValue":"old","newValue":"new"}]""", entry.Changes);
        Assert.Equal("""{"k":1}""", entry.Metadata);
        Assert.Equal("corr-1", entry.CorrelationId);
        Assert.Equal("10.0.0.1", entry.IpAddress);
        Assert.Equal("agent/1", entry.UserAgent);
    }

    [Fact]
    public async Task RecordAsync_PrefersTheDescriptorOverCollectedValuesAndOmitsChangesOnFailure()
    {
        var descriptorTenant = Guid.NewGuid();
        var writer = new RecordingWriter();
        var collector = new AuditChangeCollector();
        collector.Add("name", "old", "new");
        collector.SetTenantId(Guid.NewGuid());
        var recorder = CreateRecorder(Guid.NewGuid(), new NullAuditRequestContext(), collector, writer);

        await recorder.RecordAsync(
            new AuditDescriptor("Thing.Rename", "Thing", "id-1", "ref-1", descriptorTenant),
            AuditResult.Failed,
            "Thing.NotFound",
            createdId: "ignored");

        var entry = Assert.Single(writer.Entries);
        Assert.Equal("id-1", entry.EntityId);
        Assert.Equal("ref-1", entry.EntityReference);
        Assert.Equal(descriptorTenant, entry.TenantId);
        Assert.Null(entry.Changes);
        Assert.Equal("Thing.NotFound", entry.ErrorCode);
        Assert.Equal(AuditResult.Failed, entry.Result);
    }

    [Fact]
    public async Task RecordAsync_FallsBackToTheRequestNameAndThenToSystem()
    {
        var writer = new RecordingWriter();

        await CreateRecorder(null, new FixedRequestContext("svc@example.com"), new AuditChangeCollector(), writer)
            .RecordAsync(new AuditDescriptor("A", "T"), AuditResult.Success, null, null);
        await CreateRecorder(null, new NullAuditRequestContext(), new AuditChangeCollector(), writer)
            .RecordAsync(new AuditDescriptor("A", "T"), AuditResult.Success, null, null);
        await CreateRecorder(Guid.NewGuid(), new NullAuditRequestContext(), new AuditChangeCollector(), writer)
            .RecordAsync(new AuditDescriptor("A", "T"), AuditResult.Success, null, null);

        Assert.Collection(
            writer.Entries,
            entry => Assert.Equal((null, "svc@example.com", AuditActorType.User), (entry.ActorId, entry.ActorName, entry.ActorType)),
            entry => Assert.Equal((null, AuditActor<Guid>.SystemName, AuditActorType.System), (entry.ActorId, entry.ActorName, entry.ActorType)),
            entry =>
            {
                Assert.NotNull(entry.ActorId);
                Assert.Equal(entry.ActorId.ToString(), entry.ActorName);
            });
    }

    [Fact]
    public async Task RecordAsync_IgnoresTenantIdsOfAnotherType()
    {
        var writer = new RecordingWriter();
        var collector = new AuditChangeCollector();
        collector.SetTenantId("not-a-guid");

        await CreateRecorder(null, new NullAuditRequestContext(), collector, writer)
            .RecordAsync(new AuditDescriptor("A", "T", TenantId: 42), AuditResult.Success, null, null);

        Assert.Null(Assert.Single(writer.Entries).TenantId);
    }
}
