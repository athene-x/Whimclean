namespace Athenex.Whimclean.Auditing;

/// <summary>Default scoped <see cref="IAuditChangeCollector" />.</summary>
public sealed class AuditChangeCollector : IAuditChangeCollector
{
    private readonly List<AuditChange> _changes = [];

    /// <inheritdoc />
    public IReadOnlyList<AuditChange> Changes => _changes;

    /// <inheritdoc />
    public string? EntityReference { get; private set; }

    /// <inheritdoc />
    public object? TenantId { get; private set; }

    /// <inheritdoc />
    public void Add(string field, object? oldValue, object? newValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        _changes.Add(new AuditChange(field, oldValue, newValue));
    }

    /// <inheritdoc />
    public void SetEntityReference(string reference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);

        EntityReference = reference;
    }

    /// <inheritdoc />
    public void SetTenantId(object tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        TenantId = tenantId;
    }
}
