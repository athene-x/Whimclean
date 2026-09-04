using Athenex.Whimclean.Application.Persistence;
using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Domain.Results;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Athenex.Whimclean.Mediator.Generator.Tests;

public sealed class GeneratedMediatorTests
{
    [Fact]
    public async Task DomainEvent_ReachesTypedHandlerThroughGeneratedMediator()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider
            .GetRequiredService<IDomainEventDispatcher>();
        var sink = scope.ServiceProvider.GetRequiredService<EventSink>();
        var domainEvent = new OrderPlaced(Guid.NewGuid());

        await dispatcher.DispatchAsync(domainEvent);

        Assert.IsType<MediatorDomainEventDispatcher>(dispatcher);
        Assert.Same(domainEvent, Assert.Single(sink.Placed));
        Assert.Empty(sink.Cancelled);
    }

    [Fact]
    public async Task DomainEvent_WithoutTypedHandler_IsNoOp()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider
            .GetRequiredService<IDomainEventDispatcher>();
        var sink = scope.ServiceProvider.GetRequiredService<EventSink>();

        await dispatcher.DispatchAsync(new UnhandledEvent());

        Assert.Empty(sink.Placed);
        Assert.Empty(sink.Cancelled);
    }

    [Fact]
    public async Task Command_CommitsUnitOfWorkThroughGeneratedPipeline()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var unitOfWork = scope.ServiceProvider
            .GetRequiredService<FakeUnitOfWork>();

        var result = await sender.Send(new PlaceOrder(Guid.NewGuid()));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.Commits);
    }

    [Fact]
    public async Task FailedCommand_SkipsCommit()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var unitOfWork = scope.ServiceProvider
            .GetRequiredService<FakeUnitOfWork>();

        var result = await sender.Send(new PlaceOrder(Guid.Empty));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.Commits);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddMediator(options =>
            options.ServiceLifetime = ServiceLifetime.Scoped);
        services
            .AddMediatorDomainEventDispatcher()
            .AddUnitOfWorkBehavior();
        services.AddScoped<FakeUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp =>
            sp.GetRequiredService<FakeUnitOfWork>());
        services.AddScoped<EventSink>();
        return services.BuildServiceProvider();
    }
}

public sealed record OrderPlaced(Guid OrderId) : DomainEvent;

public sealed record OrderCancelled(Guid OrderId) : DomainEvent;

public sealed record UnhandledEvent : DomainEvent;

public sealed class EventSink
{
    public List<OrderPlaced> Placed { get; } = [];

    public List<OrderCancelled> Cancelled { get; } = [];
}

public sealed class OrderPlacedHandler(EventSink sink)
    : DomainEventHandler<OrderPlaced>
{
    protected override ValueTask HandleAsync(
        OrderPlaced domainEvent,
        CancellationToken cancellationToken)
    {
        sink.Placed.Add(domainEvent);
        return ValueTask.CompletedTask;
    }
}

public sealed class OrderCancelledHandler(EventSink sink)
    : DomainEventHandler<OrderCancelled>
{
    protected override ValueTask HandleAsync(
        OrderCancelled domainEvent,
        CancellationToken cancellationToken)
    {
        sink.Cancelled.Add(domainEvent);
        return ValueTask.CompletedTask;
    }
}

public sealed record PlaceOrder(Guid CustomerId) : ICommand<Result<Guid>>;

public sealed class PlaceOrderHandler
    : ICommandHandler<PlaceOrder, Result<Guid>>
{
    public ValueTask<Result<Guid>> Handle(
        PlaceOrder command,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(command.CustomerId == Guid.Empty
            ? Result<Guid>.Failure(
                Error.Validation("Order.Customer", "Customer is required."))
            : Result<Guid>.Success(Guid.NewGuid()));
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Commits { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Commits++;
        return Task.CompletedTask;
    }
}
