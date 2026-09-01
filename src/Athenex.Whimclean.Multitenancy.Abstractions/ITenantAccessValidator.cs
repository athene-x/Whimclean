namespace Athenex.Whimclean.Multitenancy;

/// <summary>
/// Decides whether a user belongs to a tenant. This check is what turns
/// an unverified <see cref="IRequestedTenant{TTenantId}" /> claim into an
/// established <see cref="ICurrentTenant{TTenantId}" /> — the requested
/// tenant must never be trusted without it.
/// </summary>
/// <typeparam name="TUserId">The user identifier type.</typeparam>
/// <typeparam name="TTenantId">The tenant identifier type.</typeparam>
public interface ITenantAccessValidator<TUserId, TTenantId>
    where TUserId : notnull
    where TTenantId : notnull
{
    /// <summary>
    /// Determines whether <paramref name="userId" /> is allowed to act
    /// within <paramref name="tenantId" />. Implementations should fail
    /// closed: return <see langword="false" /> for unknown users or
    /// tenants rather than guessing.
    /// </summary>
    /// <param name="userId">The user whose membership is checked.</param>
    /// <param name="tenantId">The tenant being requested.</param>
    /// <param name="cancellationToken">
    /// Propagates a request to cancel the lookup.
    /// </param>
    Task<bool> HasAccessAsync(
        TUserId userId,
        TTenantId tenantId,
        CancellationToken cancellationToken = default);
}
