using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Multitenancy;
using Microsoft.AspNetCore.Authorization;

namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Handles <see cref="TenantMemberRequirement"/>: succeeds only when an
/// authenticated user, a requested tenant, and
/// <see cref="ITenantAccessValidator{TUserId, TTenantId}"/> approval are
/// all present, and only then establishes the current tenant on the
/// scoped <see cref="TenantContext{TTenantId}"/>. This is the gate that
/// turns the unverified requested tenant into the trusted current one.
/// </summary>
/// <typeparam name="TUserId">Type of the user identifier.</typeparam>
/// <typeparam name="TTenantId">Type of the tenant identifier.</typeparam>
/// <param name="currentUser">Supplies the authenticated user's id.</param>
/// <param name="requestedTenant">
/// Supplies the unverified tenant claim recorded during resolution.
/// </param>
/// <param name="accessValidator">
/// Decides whether the user may access the requested tenant.
/// </param>
/// <param name="tenantContext">
/// The per-request holder on which the current tenant is established
/// after validation succeeds.
/// </param>
public sealed class TenantMemberAuthorizationHandler<TUserId, TTenantId>(
    ICurrentUser<TUserId> currentUser,
    IRequestedTenant<TTenantId> requestedTenant,
    ITenantAccessValidator<TUserId, TTenantId> accessValidator,
    TenantContext<TTenantId> tenantContext)
    : AuthorizationHandler<TenantMemberRequirement>
    where TUserId : notnull
    where TTenantId : notnull
{
    /// <summary>
    /// Succeeds the requirement and establishes the current tenant when
    /// the user id and requested tenant are available and the access
    /// validator approves; otherwise leaves the requirement unmet without
    /// failing the context, so other handlers may still run.
    /// </summary>
    /// <param name="context">The authorization context to update.</param>
    /// <param name="requirement">The requirement being evaluated.</param>
    /// <returns>A task that completes when evaluation finishes.</returns>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantMemberRequirement requirement)
    {
        if (!currentUser.TryGetUserId(out var userId) ||
            !requestedTenant.TryGetTenantId(out var tenantId))
        {
            return;
        }

        var hasAccess = await accessValidator.HasAccessAsync(
            userId,
            tenantId);

        if (!hasAccess)
        {
            return;
        }

        tenantContext.EstablishCurrentTenant(tenantId);
        context.Succeed(requirement);
    }
}
