namespace Athenex.Whimclean.Auditing;

/// <summary>
/// What an audited command acted on.
/// </summary>
/// <typeparam name="TTenantId">Type of the tenant identifier.</typeparam>
/// <param name="EntityType">Kind of entity, such as <c>Tenant</c> or <c>User</c>.</param>
/// <param name="EntityId">Identifier of the entity, as text.</param>
/// <param name="EntityReference">
/// A human-readable handle such as a slug, email or key, so the log reads
/// without joins.
/// </param>
/// <param name="TenantId">The tenant the action concerns, if any.</param>
public sealed record AuditTarget<TTenantId>(
    string EntityType,
    string? EntityId = null,
    string? EntityReference = null,
    TTenantId? TenantId = null)
    where TTenantId : struct;
