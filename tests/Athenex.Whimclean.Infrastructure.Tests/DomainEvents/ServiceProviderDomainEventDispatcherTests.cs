using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Infrastructure.Tests.DomainEvents;

public sealed class ServiceProviderDomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_InvokesEveryHandlerForTheEventType()
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatcher();
        services.AddDomainEventHandler<TestDomainEvent, FirstHandler>();
        services.AddDomainEventHandler<TestDomainEvent, SecondHandler>();
        services.AddSingleton<HandledEvents>();

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider
            .GetRequiredService<IDomainEventDispatcher>();
        var domainEvent = new TestDomainEvent(Guid.NewGuid());

        await dispatcher.DispatchAsync(domainEvent);

        var handled = scope.ServiceProvider.GetRequiredService<HandledEvents>();
        Assert.Equal(
            [nameof(FirstHandler), nameof(SecondHandler)],
            handled.HandlerNames);
        Assert.All(
            handled.Events,
            handledEvent => Assert.Equal(domainEvent, handledEvent));
    }

    [Fact]
    public async Task DispatchAsync_WithoutHandlers_Succeeds()
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatcher();

        await using var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider
            .GetRequiredService<IDomainEventDispatcher>();

        await dispatcher.DispatchAsync(new TestDomainEvent(Guid.NewGuid()));
    }

    [Fact]
    public async Task DispatchAsync_UsesRuntimeEventType()
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatcher();
        services.AddDomainEventHandler<TestDomainEvent, FirstHandler>();
        services.AddSingleton<HandledEvents>();

        await using var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider
            .GetRequiredService<IDomainEventDispatcher>();

        // Dispatch through the base interface; the handler registered for
        // the concrete event type must still be invoked.
        IDomainEvent domainEvent = new TestDomainEvent(Guid.NewGuid());
        await dispatcher.DispatchAsync(domainEvent);

        var handled = serviceProvider.GetRequiredService<HandledEvents>();
        Assert.Single(handled.Events);
    }

    private sealed record TestDomainEvent(Guid Id) : DomainEvent;

    private sealed class HandledEvents
    {
        public List<string> HandlerNames { get; } = [];
        public List<IDomainEvent> Events { get; } = [];
    }

    private sealed class FirstHandler(HandledEvents handled)
        : IDomainEventHandler<TestDomainEvent>
    {
        public Task HandleAsync(
            TestDomainEvent domainEvent,
            CancellationToken cancellationToken = default)
        {
            handled.HandlerNames.Add(nameof(FirstHandler));
            handled.Events.Add(domainEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class SecondHandler(HandledEvents handled)
        : IDomainEventHandler<TestDomainEvent>
    {
        public Task HandleAsync(
            TestDomainEvent domainEvent,
            CancellationToken cancellationToken = default)
        {
            handled.HandlerNames.Add(nameof(SecondHandler));
            handled.Events.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
