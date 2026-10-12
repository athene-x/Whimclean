namespace Athenex.Whimclean.Auditing.Tests;

public sealed class AuditLogTests
{
    [Fact]
    public void Create_TrimsAndTruncatesText()
    {
        var entry = AuditLog<Guid, Guid>.Create(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            new AuditActor<Guid>(null, "  " + new string('a', 300), AuditActorType.Service),
            "  Tenant.Create  ",
            new AuditTarget<Guid>("Tenant", EntityReference: new string('r', 400)),
            AuditResult.Success,
            request: new AuditRequestInfo(UserAgent: new string('u', 600)));

        Assert.Equal(AuditLog<Guid, Guid>.MaximumActorNameLength, entry.ActorName.Length);
        Assert.Equal("Tenant.Create", entry.Action);
        Assert.Equal(AuditLog<Guid, Guid>.MaximumEntityReferenceLength, entry.EntityReference!.Length);
        Assert.Equal(AuditLog<Guid, Guid>.MaximumUserAgentLength, entry.UserAgent!.Length);
        Assert.Null(entry.CorrelationId);
        Assert.Null(entry.EntityId);
    }

    [Fact]
    public void Create_UsesAnonymousForABlankActorName()
    {
        var entry = AuditLog<Guid, Guid>.Create(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            new AuditActor<Guid>(null, " ", AuditActorType.User),
            "A",
            new AuditTarget<Guid>("T"),
            AuditResult.Failed,
            errorCode: "X");

        Assert.Equal(AuditActor<Guid>.AnonymousName, entry.ActorName);
        Assert.Equal("X", entry.ErrorCode);
    }

    [Fact]
    public void Create_RejectsBlankActionOrEntityType()
    {
        Assert.Throws<ArgumentException>(() => AuditLog<Guid, Guid>.Create(
            Guid.NewGuid(), DateTimeOffset.UtcNow, AuditActor<Guid>.System, " ", new AuditTarget<Guid>("T"), AuditResult.Success));
        Assert.Throws<ArgumentException>(() => AuditLog<Guid, Guid>.Create(
            Guid.NewGuid(), DateTimeOffset.UtcNow, AuditActor<Guid>.System, "A", new AuditTarget<Guid>(" "), AuditResult.Success));
    }
}
