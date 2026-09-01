using Microsoft.AspNetCore.Authorization;

namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Authorization requirement demanding that the authenticated user is a
/// member of the requested tenant. Satisfied by
/// <see cref="TenantMemberAuthorizationHandler{TUserId, TTenantId}"/>,
/// which also establishes the current tenant on success; used by the
/// <see cref="MultitenancyPolicies.TenantMember"/> policy.
/// </summary>
public sealed class TenantMemberRequirement : IAuthorizationRequirement;
