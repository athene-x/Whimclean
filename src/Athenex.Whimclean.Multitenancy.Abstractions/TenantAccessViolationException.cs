namespace Athenex.Whimclean.Multitenancy;

/// <summary>
/// Thrown by the EF infrastructure when a save would modify an
/// <see cref="IMultiTenant{TTenantId}" /> record owned by a tenant other
/// than the established current tenant. The write is rejected instead of
/// leaking across the tenant boundary.
/// </summary>
public sealed class TenantAccessViolationException()
    : Exception("A tenant-owned record cannot be changed by another tenant.");
