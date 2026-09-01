using Athenex.Whimclean.Infrastructure.Auditing;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Infrastructure.Tests.Auditing;

public sealed class AuditActorResolverTests
{
    [Fact]
    public void IdentityResolver_ReturnsTheUserIdUnchanged()
    {
        var userId = Guid.NewGuid();
        var resolver = new IdentityAuditActorResolver<Guid>();

        Assert.Equal(userId, resolver.Resolve(userId));
    }

    [Fact]
    public void NullableResolver_LiftsTheUserIdToNonNullNullable()
    {
        var userId = Guid.NewGuid();
        var resolver = new NullableAuditActorResolver<Guid>();

        Guid? actor = resolver.Resolve(userId);

        Assert.Equal(userId, actor);
    }

    [Fact]
    public void AddIdentityAuditActorResolver_RegistersTheSingleton()
    {
        var services = new ServiceCollection();
        services.AddIdentityAuditActorResolver<Guid>();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.IsType<IdentityAuditActorResolver<Guid>>(
            serviceProvider
                .GetRequiredService<IAuditActorResolver<Guid, Guid>>());
    }

    [Fact]
    public void AddNullableAuditActorResolver_RegistersTheSingleton()
    {
        var services = new ServiceCollection();
        services.AddNullableAuditActorResolver<Guid>();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.IsType<NullableAuditActorResolver<Guid>>(
            serviceProvider
                .GetRequiredService<IAuditActorResolver<Guid, Guid?>>());
    }

    [Fact]
    public void AddAuditActorResolver_LeavesAnExistingRegistrationInPlace()
    {
        var services = new ServiceCollection();
        services.AddSingleton<
            IAuditActorResolver<Guid, Guid?>,
            CustomResolver>();
        services.AddNullableAuditActorResolver<Guid>();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.IsType<CustomResolver>(
            serviceProvider
                .GetRequiredService<IAuditActorResolver<Guid, Guid?>>());
    }

    private sealed class CustomResolver : IAuditActorResolver<Guid, Guid?>
    {
        public Guid? Resolve(Guid userId) => userId;
    }
}
