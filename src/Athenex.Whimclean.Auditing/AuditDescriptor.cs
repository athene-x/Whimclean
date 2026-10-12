namespace Athenex.Whimclean.Auditing;

/// <summary>
/// What a command does, for the audit log. Commands that create an entity
/// leave <see cref="EntityId" /> null; the behavior fills it from a
/// successful <c>Result&lt;TId&gt;</c> whose value is a <see cref="Guid" />,
/// string or number. Domain event handlers may complete
/// <see cref="EntityReference" /> and <see cref="TenantId" /> through the
/// <see cref="IAuditChangeCollector" />.
/// </summary>
/// <param name="Action">The command, such as <c>Tenant.Suspend</c>.</param>
/// <param name="EntityType">Kind of entity acted on.</param>
/// <param name="EntityId">Identifier of the entity acted on, as text.</param>
/// <param name="EntityReference">Human-readable handle of the entity.</param>
/// <param name="TenantId">
/// The tenant the action concerns, of the tenant identifier type the
/// application registered with <c>AddAuditing</c>; values of another type
/// are ignored.
/// </param>
/// <param name="Reason">Reason the caller gave.</param>
/// <param name="Metadata">
/// Command-specific extras, serialized as a JSON object.
/// </param>
public sealed record AuditDescriptor(
    string Action,
    string EntityType,
    string? EntityId = null,
    string? EntityReference = null,
    object? TenantId = null,
    string? Reason = null,
    IReadOnlyDictionary<string, object?>? Metadata = null);
