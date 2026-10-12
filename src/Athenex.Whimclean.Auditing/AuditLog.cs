using Athenex.Whimclean.Domain.Entities;

namespace Athenex.Whimclean.Auditing;

/// <summary>
/// One recorded command outcome. Text fields are trimmed and truncated to
/// the declared maximum lengths so a hostile user agent or reason can
/// never fail the write; <see cref="Changes" /> and
/// <see cref="Metadata" /> hold JSON documents the application maps to a
/// JSON column. The application owns the table mapping and migration.
/// </summary>
/// <typeparam name="TUserId">Type of the local user identifier.</typeparam>
/// <typeparam name="TTenantId">Type of the tenant identifier.</typeparam>
public sealed class AuditLog<TUserId, TTenantId> : Entity<Guid>
    where TUserId : struct
    where TTenantId : struct
{
    /// <summary>Maximum stored length of <see cref="ActorName" />.</summary>
    public const int MaximumActorNameLength = 200;

    /// <summary>Maximum stored length of <see cref="Action" />.</summary>
    public const int MaximumActionLength = 100;

    /// <summary>Maximum stored length of <see cref="EntityType" />.</summary>
    public const int MaximumEntityTypeLength = 100;

    /// <summary>Maximum stored length of <see cref="EntityId" />.</summary>
    public const int MaximumEntityIdLength = 100;

    /// <summary>Maximum stored length of <see cref="EntityReference" />.</summary>
    public const int MaximumEntityReferenceLength = 300;

    /// <summary>Maximum stored length of <see cref="Reason" />.</summary>
    public const int MaximumReasonLength = 1000;

    /// <summary>Maximum stored length of <see cref="ErrorCode" />.</summary>
    public const int MaximumErrorCodeLength = 200;

    /// <summary>Maximum stored length of <see cref="CorrelationId" />.</summary>
    public const int MaximumCorrelationIdLength = 100;

    /// <summary>Maximum stored length of <see cref="IpAddress" />.</summary>
    public const int MaximumIpAddressLength = 45;

    /// <summary>Maximum stored length of <see cref="UserAgent" />.</summary>
    public const int MaximumUserAgentLength = 500;

    private AuditLog()
    {
    }

    private AuditLog(Guid id) : base(id)
    {
    }

    /// <summary>When the command finished, in UTC.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>The tenant the action concerns, if any.</summary>
    public TTenantId? TenantId { get; private set; }

    /// <summary>The local user who acted, if known.</summary>
    public TUserId? ActorId { get; private set; }

    /// <summary>Display name of the actor.</summary>
    public string ActorName { get; private set; } = null!;

    /// <summary>Kind of actor.</summary>
    public AuditActorType ActorType { get; private set; }

    /// <summary>The command, such as <c>Tenant.Suspend</c>.</summary>
    public string Action { get; private set; } = null!;

    /// <summary>Kind of entity acted on.</summary>
    public string EntityType { get; private set; } = null!;

    /// <summary>Identifier of the entity acted on, as text.</summary>
    public string? EntityId { get; private set; }

    /// <summary>Human-readable handle of the entity acted on.</summary>
    public string? EntityReference { get; private set; }

    /// <summary>JSON array of field changes, for successful commands.</summary>
    public string? Changes { get; private set; }

    /// <summary>Reason the caller gave, if the command carries one.</summary>
    public string? Reason { get; private set; }

    /// <summary>Outcome of the command.</summary>
    public AuditResult Result { get; private set; }

    /// <summary>Error code of a failed command, or the exception type name.</summary>
    public string? ErrorCode { get; private set; }

    /// <summary>Identifier tying the entry to request logs.</summary>
    public string? CorrelationId { get; private set; }

    /// <summary>Remote address of the caller.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>User agent of the caller.</summary>
    public string? UserAgent { get; private set; }

    /// <summary>JSON object of command-specific extras.</summary>
    public string? Metadata { get; private set; }

    /// <summary>
    /// Creates an entry, trimming and truncating text to the maximum
    /// lengths and normalizing <paramref name="occurredAt" /> to UTC.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="action" /> or the target's entity type is blank, or
    /// <paramref name="id" /> is empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="actor" /> or <paramref name="target" /> is
    /// <see langword="null" />.
    /// </exception>
    public static AuditLog<TUserId, TTenantId> Create(
        Guid id,
        DateTimeOffset occurredAt,
        AuditActor<TUserId> actor,
        string action,
        AuditTarget<TTenantId> target,
        AuditResult result,
        string? errorCode = null,
        string? changes = null,
        string? reason = null,
        string? metadata = null,
        AuditRequestInfo? request = null)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.EntityType);

        request ??= AuditRequestInfo.None;

        return new AuditLog<TUserId, TTenantId>(id)
        {
            OccurredAt = occurredAt.ToUniversalTime(),
            TenantId = target.TenantId,
            ActorId = actor.Id,
            ActorName = Truncate(actor.Name, MaximumActorNameLength) ?? AuditActor<TUserId>.AnonymousName,
            ActorType = actor.Type,
            Action = Truncate(action, MaximumActionLength)!,
            EntityType = Truncate(target.EntityType, MaximumEntityTypeLength)!,
            EntityId = Truncate(target.EntityId, MaximumEntityIdLength),
            EntityReference = Truncate(target.EntityReference, MaximumEntityReferenceLength),
            Changes = changes,
            Reason = Truncate(reason, MaximumReasonLength),
            Result = result,
            ErrorCode = Truncate(errorCode, MaximumErrorCodeLength),
            CorrelationId = Truncate(request.CorrelationId, MaximumCorrelationIdLength),
            IpAddress = Truncate(request.IpAddress, MaximumIpAddressLength),
            UserAgent = Truncate(request.UserAgent, MaximumUserAgentLength),
            Metadata = metadata
        };
    }

    private static string? Truncate(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        return trimmed.Length <= maximumLength ? trimmed : trimmed[..maximumLength];
    }
}
