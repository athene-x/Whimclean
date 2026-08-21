namespace Athenex.Whimclean.Multitenancy;

public sealed class CurrentTenantRequiredException()
    : Exception("A current tenant is required to persist tenant-owned changes.");
