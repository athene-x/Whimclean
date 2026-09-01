using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy.Tests;

public sealed class TenantResolutionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_RecordsTheResolvedTenantAndContinues()
    {
        var tenantId = Guid.NewGuid();
        var pipelineContinued = false;
        var tenantContext = new TenantContext<Guid>();
        var middleware = new TenantResolutionMiddleware<Guid>(_ =>
        {
            pipelineContinued = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(
            new DefaultHttpContext(),
            new FixedResolver(tenantId),
            tenantContext);

        Assert.True(pipelineContinued);
        Assert.True(tenantContext.TryGetRequestedTenantId(out var requested));
        Assert.Equal(tenantId, requested);
        // Resolution records only a claim; it never establishes the
        // current tenant.
        Assert.False(tenantContext.TryGetTenantId(out _));
    }

    [Fact]
    public async Task InvokeAsync_WithoutResolvableTenant_StillContinues()
    {
        var pipelineContinued = false;
        var tenantContext = new TenantContext<Guid>();
        var middleware = new TenantResolutionMiddleware<Guid>(_ =>
        {
            pipelineContinued = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(
            new DefaultHttpContext(),
            new FixedResolver(tenantId: null),
            tenantContext);

        Assert.True(pipelineContinued);
        Assert.False(tenantContext.TryGetRequestedTenantId(out _));
    }

    private sealed class FixedResolver(Guid? tenantId)
        : IRequestedTenantResolver<Guid>
    {
        public bool TryResolve(
            HttpContext httpContext,
            out Guid resolvedTenantId)
        {
            if (tenantId is Guid value)
            {
                resolvedTenantId = value;
                return true;
            }

            resolvedTenantId = default;
            return false;
        }
    }
}
