namespace Athenex.Whimclean.Auditing;

/// <summary>
/// Scoped per command. Domain event handlers add the field changes, the
/// entity reference and the tenant they know about; the audit behavior
/// reads them after the command commits.
/// </summary>
public interface IAuditChangeCollector
{
    /// <summary>Changes added so far, in order.</summary>
    IReadOnlyList<AuditChange> Changes { get; }

    /// <summary>Entity reference set by a handler, if any.</summary>
    string? EntityReference { get; }

    /// <summary>
    /// Tenant set by a handler, if any; of the registered tenant
    /// identifier type.
    /// </summary>
    object? TenantId { get; }

    /// <summary>Records that <paramref name="field" /> changed.</summary>
    void Add(string field, object? oldValue, object? newValue);

    /// <summary>Sets the entity reference for the entry.</summary>
    void SetEntityReference(string reference);

    /// <summary>Sets the tenant for the entry.</summary>
    void SetTenantId(object tenantId);
}
