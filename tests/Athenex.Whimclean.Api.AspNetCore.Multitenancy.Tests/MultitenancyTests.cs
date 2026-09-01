using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Multitenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy.Tests;

public sealed class MultitenancyTests
{
    [Fact]
    public void HeaderResolver_ParsesConfiguredTenantIdentifier()
    {
        var tenantId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Organization"] = tenantId.ToString();
        var resolver = new HeaderRequestedTenantResolver<Guid>(
            Options.Create(new HeaderTenantResolutionOptions
            {
                HeaderName = "X-Organization"
            }));

        var resolved = resolver.TryResolve(context, out var resolvedTenantId);

        Assert.True(resolved);
        Assert.Equal(tenantId, resolvedTenantId);
    }

    [Fact]
    public void HeaderResolver_RejectsRepeatedTenantHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = new[]
        {
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString()
        };
        var resolver = new HeaderRequestedTenantResolver<Guid>(
            Options.Create(new HeaderTenantResolutionOptions()));

        var resolved = resolver.TryResolve(context, out _);

        Assert.False(resolved);
    }

    [Fact]
    public async Task TenantMemberHandler_EstablishesAuthorizedTenant()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var tenantContext = new TenantContext<Guid>();
        var requirement = new TenantMemberRequirement();
        var authorizationContext = new AuthorizationHandlerContext(
            [requirement],
            new System.Security.Claims.ClaimsPrincipal(),
            resource: null);
        var handler = new TenantMemberAuthorizationHandler<Guid, Guid>(
            new FixedCurrentUser(userId),
            new FixedRequestedTenant(tenantId),
            new FixedAccessValidator(hasAccess: true),
            tenantContext);

        await handler.HandleAsync(authorizationContext);

        Assert.True(authorizationContext.HasSucceeded);
        Assert.True(tenantContext.TryGetTenantId(out var currentTenantId));
        Assert.Equal(tenantId, currentTenantId);
    }

    [Fact]
    public async Task TenantMemberHandler_DoesNotEstablishUnauthorizedTenant()
    {
        var tenantContext = new TenantContext<Guid>();
        var requirement = new TenantMemberRequirement();
        var authorizationContext = new AuthorizationHandlerContext(
            [requirement],
            new System.Security.Claims.ClaimsPrincipal(),
            resource: null);
        var handler = new TenantMemberAuthorizationHandler<Guid, Guid>(
            new FixedCurrentUser(Guid.NewGuid()),
            new FixedRequestedTenant(Guid.NewGuid()),
            new FixedAccessValidator(hasAccess: false),
            tenantContext);

        await handler.HandleAsync(authorizationContext);

        Assert.False(authorizationContext.HasSucceeded);
        Assert.False(tenantContext.TryGetTenantId(out _));
    }

    [Theory]
    [InlineData(typeof(CurrentTenantRequiredException), "Tenancy.CurrentTenantRequired")]
    [InlineData(typeof(TenantAccessViolationException), "Tenancy.AccessDenied")]
    public void ExceptionMapper_MapsTenantException(
        Type exceptionType,
        string expectedCode)
    {
        var exception = Assert.IsAssignableFrom<Exception>(
            Activator.CreateInstance(exceptionType));
        var mapper = new TenantApiExceptionMapper();

        var mapped = mapper.TryMap(exception, out var mapping);

        Assert.True(mapped);
        Assert.Equal(StatusCodes.Status403Forbidden, mapping.StatusCode);
        Assert.Equal(expectedCode, mapping.Response.Code);
    }

    private sealed class FixedCurrentUser(Guid userId)
        : ICurrentUser<Guid>
    {
        public bool TryGetUserId(out Guid resolvedUserId)
        {
            resolvedUserId = userId;
            return true;
        }
    }

    private sealed class FixedRequestedTenant(Guid tenantId)
        : IRequestedTenant<Guid>
    {
        public bool TryGetTenantId(out Guid resolvedTenantId)
        {
            resolvedTenantId = tenantId;
            return true;
        }
    }

    private sealed class FixedAccessValidator(bool hasAccess)
        : ITenantAccessValidator<Guid, Guid>
    {
        public Task<bool> HasAccessAsync(
            Guid userId,
            Guid tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(hasAccess);
    }
}
