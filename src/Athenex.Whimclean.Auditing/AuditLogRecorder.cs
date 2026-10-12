using Athenex.Whimclean.Application.Context;

namespace Athenex.Whimclean.Auditing;

/// <summary>
/// Default <see cref="IAuditLogRecorder" />: resolves the actor from the
/// current user and request context, merges the descriptor with what
/// the <see cref="IAuditChangeCollector" /> gathered, and hands the entry
/// to the <see cref="IAuditLogWriter{TUserId, TTenantId}" />.
/// </summary>
/// <typeparam name="TUserId">Type of the local user identifier.</typeparam>
/// <typeparam name="TTenantId">Type of the tenant identifier.</typeparam>
public sealed class AuditLogRecorder<TUserId, TTenantId>(
    ICurrentUser<TUserId> currentUser,
    IAuditRequestContext requestContext,
    IAuditChangeCollector changes,
    IAuditLogWriter<TUserId, TTenantId> writer,
    TimeProvider timeProvider)
    : IAuditLogRecorder
    where TUserId : struct
    where TTenantId : struct
{
    /// <inheritdoc />
    public ValueTask RecordAsync(
        AuditDescriptor descriptor,
        AuditResult result,
        string? errorCode,
        string? createdId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var entry = AuditLog<TUserId, TTenantId>.Create(
            Guid.CreateVersion7(),
            timeProvider.GetUtcNow(),
            ResolveActor(),
            descriptor.Action,
            new AuditTarget<TTenantId>(
                descriptor.EntityType,
                descriptor.EntityId ?? createdId,
                descriptor.EntityReference ?? changes.EntityReference,
                AsTenantId(descriptor.TenantId) ?? AsTenantId(changes.TenantId)),
            result,
            errorCode,
            result == AuditResult.Success && changes.Changes.Count > 0
                ? AuditJson.Serialize(changes.Changes)
                : null,
            descriptor.Reason,
            AuditJson.Serialize(descriptor.Metadata),
            new AuditRequestInfo(
                requestContext.CorrelationId,
                requestContext.IpAddress,
                requestContext.UserAgent));

        return writer.WriteAsync(entry, cancellationToken);
    }

    private AuditActor<TUserId> ResolveActor()
    {
        if (currentUser.TryGetUserId(out var userId))
        {
            return new AuditActor<TUserId>(
                userId,
                requestContext.ActorName ?? userId.ToString() ?? AuditActor<TUserId>.AnonymousName,
                AuditActorType.User);
        }

        return requestContext.ActorName is { } name
            ? new AuditActor<TUserId>(null, name, AuditActorType.User)
            : AuditActor<TUserId>.System;
    }

    private static TTenantId? AsTenantId(object? value) =>
        value is TTenantId tenantId ? tenantId : null;
}
