using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Auditing.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddAuditing_RegistersTheBehaviorRecorderCollectorAndNullContext()
    {
        var services = new ServiceCollection();

        services.AddAuditing<Guid, Guid>();
        services.AddAuditing<Guid, Guid>();

        Assert.Single(services, descriptor =>
            descriptor.ServiceType == typeof(IPipelineBehavior<,>) &&
            descriptor.ImplementationType == typeof(AuditCommandBehavior<,>));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IAuditLogRecorder) &&
            descriptor.ImplementationType == typeof(AuditLogRecorder<Guid, Guid>));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IAuditChangeCollector) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IAuditRequestContext) &&
            descriptor.ImplementationType == typeof(NullAuditRequestContext));
    }

    [Fact]
    public void AddAuditing_KeepsARegisteredRequestContext()
    {
        var services = new ServiceCollection();
        services.AddScoped<IAuditRequestContext>(_ => new FixedRequestContext("x"));

        services.AddAuditing<Guid, Guid>();

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IAuditRequestContext));
    }
}
