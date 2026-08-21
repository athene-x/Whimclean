namespace Athenex.Whimclean.Multitenancy;

public interface ITenantAccessValidator<TUserId, TTenantId>
    where TUserId : notnull
    where TTenantId : notnull
{
    Task<bool> HasAccessAsync(
        TUserId userId,
        TTenantId tenantId,
        CancellationToken cancellationToken = default);
}
