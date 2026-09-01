using Athenex.Whimclean.Multitenancy;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy.Tests;

public sealed class AmbientCurrentTenantTests
{
    [Fact]
    public void TryGetTenantId_WithoutScope_ResolvesNoTenant()
    {
        var currentTenant = new AmbientCurrentTenant<Guid>();

        Assert.False(currentTenant.TryGetTenantId(out _));
    }

    [Fact]
    public void TryGetTenantId_InsideScope_ResolvesScopedTenant()
    {
        var currentTenant = new AmbientCurrentTenant<Guid>();
        var tenantId = Guid.NewGuid();

        using (AmbientCurrentTenant<Guid>.Begin(tenantId))
        {
            Assert.True(currentTenant.TryGetTenantId(out var resolved));
            Assert.Equal(tenantId, resolved);
        }

        Assert.False(currentTenant.TryGetTenantId(out _));
    }

    [Fact]
    public void Begin_NestsAndRestoresOuterTenant()
    {
        var currentTenant = new AmbientCurrentTenant<Guid>();
        var outerTenantId = Guid.NewGuid();
        var innerTenantId = Guid.NewGuid();

        using (AmbientCurrentTenant<Guid>.Begin(outerTenantId))
        {
            using (AmbientCurrentTenant<Guid>.Begin(innerTenantId))
            {
                Assert.True(currentTenant.TryGetTenantId(out var inner));
                Assert.Equal(innerTenantId, inner);
            }

            Assert.True(currentTenant.TryGetTenantId(out var outer));
            Assert.Equal(outerTenantId, outer);
        }
    }

    [Fact]
    public async Task Begin_FlowsAcrossAwaitsButNotToSiblings()
    {
        var currentTenant = new AmbientCurrentTenant<Guid>();
        var tenantId = Guid.NewGuid();

        using (AmbientCurrentTenant<Guid>.Begin(tenantId))
        {
            await Task.Yield();

            Assert.True(currentTenant.TryGetTenantId(out var resolved));
            Assert.Equal(tenantId, resolved);
        }

        await Task.Run(() =>
            Assert.False(currentTenant.TryGetTenantId(out _)));
    }
}
