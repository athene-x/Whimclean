using Athenex.Whimclean.Application.Persistence;
using Athenex.Whimclean.Domain.Results;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Athenex.Whimclean.Mediator.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddMediatorDomainEventDispatcher_RegistersDispatcher()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPublisher, RecordingPublisher>();

        services.AddMediatorDomainEventDispatcher();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.IsType<MediatorDomainEventDispatcher>(
            scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>());
    }

    [Fact]
    public void AddMediatorDomainEventDispatcher_LeavesExistingInPlace()
    {
        var services = new ServiceCollection();
        services.AddScoped<
            IDomainEventDispatcher,
            ServiceProviderDomainEventDispatcher>();

        services.AddMediatorDomainEventDispatcher();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.IsType<ServiceProviderDomainEventDispatcher>(
            scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>());
    }

    [Fact]
    public void AddUnitOfWorkBehavior_Twice_KeepsSingleRegistration()
    {
        var services = new ServiceCollection();
        services.AddScoped<IUnitOfWork, FakeUnitOfWork>();

        services.AddUnitOfWorkBehavior();
        services.AddUnitOfWorkBehavior();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Single(scope.ServiceProvider
            .GetServices<IPipelineBehavior<TestCommand, Result>>());
    }

    [Fact]
    public void AddUnitOfWorkBehavior_DoesNotBindToQueries()
    {
        var services = new ServiceCollection();
        services.AddScoped<IUnitOfWork, FakeUnitOfWork>();

        services.AddUnitOfWorkBehavior();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Empty(scope.ServiceProvider
            .GetServices<IPipelineBehavior<TestQuery, Guid>>());
    }
}
