using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Multitenancy;
using Microsoft.AspNetCore.Authorization;

namespace Athenex.Whimclean.Api.FastEndpoints.Multitenancy;

public sealed class TenantMemberAuthorizationHandler<TUserId, TTenantId>(
    ICurrentUser<TUserId> currentUser,
    IRequestedTenant<TTenantId> requestedTenant,
    ITenantAccessValidator<TUserId, TTenantId> accessValidator,
    TenantContext<TTenantId> tenantContext)
    : AuthorizationHandler<TenantMemberRequirement>
    where TUserId : notnull
    where TTenantId : notnull
{
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
