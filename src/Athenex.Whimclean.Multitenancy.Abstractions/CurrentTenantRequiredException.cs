namespace Athenex.Whimclean.Multitenancy;

/// <summary>
/// Thrown by the EF infrastructure when tenant-owned changes are about to
/// be persisted but no current tenant is established (i.e.
/// <see cref="ICurrentTenant{TTenantId}.TryGetTenantId" /> returned
/// <see langword="false" />). Persisting fails rather than writing
/// unattributed tenant data.
/// </summary>
public sealed class CurrentTenantRequiredException()
    : Exception("A current tenant is required to persist tenant-owned changes.");
