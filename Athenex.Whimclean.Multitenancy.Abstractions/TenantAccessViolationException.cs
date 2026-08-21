namespace Athenex.Whimclean.Multitenancy;

public sealed class TenantAccessViolationException()
    : Exception("A tenant-owned record cannot be changed by another tenant.");
