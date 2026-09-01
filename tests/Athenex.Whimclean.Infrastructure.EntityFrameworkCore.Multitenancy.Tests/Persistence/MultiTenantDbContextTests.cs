using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Domain.Auditables;
using Athenex.Whimclean.Domain.Behaviors;
using Athenex.Whimclean.Domain.Entities;
using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Infrastructure.Auditing;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy.Persistence;
using Athenex.Whimclean.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy.Tests.Persistence;

public sealed class MultiTenantDbContextTests
{
    [Fact]
    public async Task QueryFilter_ReturnsOnlyCurrentTenantRecords()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var tenantOneId = Guid.NewGuid();
        var tenantTwoId = Guid.NewGuid();

        await using (var tenantOneContext = CreateContext(
                         databaseRoot,
                         tenantOneId))
        {
            tenantOneContext.Aggregates.Add(
                new TenantAggregate(Guid.NewGuid(), tenantOneId));
            await tenantOneContext.SaveChangesAsync();
        }

        await using (var tenantTwoContext = CreateContext(
                         databaseRoot,
                         tenantTwoId))
        {
            tenantTwoContext.Aggregates.Add(
                new TenantAggregate(Guid.NewGuid(), tenantTwoId));
            await tenantTwoContext.SaveChangesAsync();
        }

        await using var queryContext = CreateContext(
            databaseRoot,
            tenantOneId);

        var records = await queryContext.Aggregates.ToArrayAsync();

        var record = Assert.Single(records);
        Assert.Equal(tenantOneId, record.TenantId);
    }

    [Fact]
    public async Task QueryFilter_WithoutCurrentTenant_ReturnsNoRecords()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var tenantId = Guid.NewGuid();

        await using (var seedContext = CreateContext(databaseRoot, tenantId))
        {
            seedContext.Aggregates.Add(
                new TenantAggregate(Guid.NewGuid(), tenantId));
            await seedContext.SaveChangesAsync();
        }

        await using var queryContext = CreateContext(
            databaseRoot,
            tenantId: null);

        var records = await queryContext.Aggregates.ToArrayAsync();

        Assert.Empty(records);
    }

    [Fact]
    public async Task NamedQueryFilters_CanBeDisabledIndependently()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var tenantOneId = Guid.NewGuid();
        var tenantTwoId = Guid.NewGuid();

        await using (var tenantOneContext = CreateContext(
                         databaseRoot,
                         tenantOneId))
        {
            tenantOneContext.Aggregates.AddRange(
                new TenantAggregate(Guid.NewGuid(), tenantOneId),
                new TenantAggregate(
                    Guid.NewGuid(),
                    tenantOneId,
                    isDeleted: true));
            await tenantOneContext.SaveChangesAsync();
        }

        await using (var tenantTwoContext = CreateContext(
                         databaseRoot,
                         tenantTwoId))
        {
            tenantTwoContext.Aggregates.Add(
                new TenantAggregate(Guid.NewGuid(), tenantTwoId));
            await tenantTwoContext.SaveChangesAsync();
        }

        await using var queryContext = CreateContext(
            databaseRoot,
            tenantOneId);

        var includingDeleted = await queryContext.Aggregates
            .IgnoreQueryFilters(["SoftDeleteFilter"])
            .ToArrayAsync();
        var acrossTenants = await queryContext.Aggregates
            .IgnoreQueryFilters(["TenantFilter"])
            .ToArrayAsync();

        Assert.Equal(2, includingDeleted.Length);
        Assert.All(
            includingDeleted,
            aggregate => Assert.Equal(tenantOneId, aggregate.TenantId));
        Assert.Equal(2, acrossTenants.Length);
        Assert.All(acrossTenants, aggregate => Assert.False(aggregate.IsDeleted));
    }

    [Fact]
    public async Task IncludeSoftDeleted_RevealsDeletedButKeepsTenantIsolation()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var tenantOneId = Guid.NewGuid();
        var tenantTwoId = Guid.NewGuid();

        await using (var tenantOneContext = CreateContext(
                         databaseRoot,
                         tenantOneId))
        {
            tenantOneContext.Aggregates.AddRange(
                new TenantAggregate(Guid.NewGuid(), tenantOneId),
                new TenantAggregate(
                    Guid.NewGuid(),
                    tenantOneId,
                    isDeleted: true));
            await tenantOneContext.SaveChangesAsync();
        }

        await using (var tenantTwoContext = CreateContext(
                         databaseRoot,
                         tenantTwoId))
        {
            tenantTwoContext.Aggregates.Add(
                new TenantAggregate(Guid.NewGuid(), tenantTwoId));
            await tenantTwoContext.SaveChangesAsync();
        }

        await using var queryContext = CreateContext(
            databaseRoot,
            tenantOneId);

        var records = await queryContext.Aggregates
            .IncludeSoftDeleted()
            .ToArrayAsync();

        Assert.Equal(2, records.Length);
        Assert.All(
            records,
            aggregate => Assert.Equal(tenantOneId, aggregate.TenantId));
        Assert.Contains(records, aggregate => aggregate.IsDeleted);
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsCrossTenantWrite()
    {
        await using var context = CreateContext(
            new InMemoryDatabaseRoot(),
            Guid.NewGuid());

        context.Aggregates.Add(
            new TenantAggregate(Guid.NewGuid(), Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => context.SaveChangesAsync());

        Assert.Contains("another tenant", exception.Message);
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsReassignmentIntoTheCurrentTenant()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var tenantOneId = Guid.NewGuid();
        var tenantTwoId = Guid.NewGuid();

        await using (var seedContext = CreateContext(databaseRoot, tenantOneId))
        {
            seedContext.Aggregates.Add(
                new TenantAggregate(Guid.NewGuid(), tenantOneId));
            await seedContext.SaveChangesAsync();
        }

        await using var capturingContext = CreateContext(
            databaseRoot,
            tenantTwoId);

        // Reach the foreign row the way an administrative query would,
        // then try to capture it by rewriting TenantId to the current
        // tenant — the current-value check alone would let this through.
        var foreignAggregate = await capturingContext.Aggregates
            .IgnoreQueryFilters(["TenantFilter"])
            .SingleAsync();
        foreignAggregate.MoveToTenant(tenantTwoId);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => capturingContext.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutCurrentTenant_RejectsWrite()
    {
        await using var context = CreateContext(
            new InMemoryDatabaseRoot(),
            tenantId: null);

        context.Aggregates.Add(
            new TenantAggregate(Guid.NewGuid(), Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<CurrentTenantRequiredException>(
            () => context.SaveChangesAsync());

        Assert.Contains("current tenant", exception.Message);
    }

    private static TestMultiTenantDbContext CreateContext(
        InMemoryDatabaseRoot databaseRoot,
        Guid? tenantId)
    {
        var options = new DbContextOptionsBuilder<TestMultiTenantDbContext>()
            .UseInMemoryDatabase("multi-tenant-tests", databaseRoot)
            .Options;

        return new TestMultiTenantDbContext(
            options,
            TimeProvider.System,
            new FixedCurrentUser(Guid.NewGuid()),
            new NullableAuditActorResolver<Guid>(),
            new NoOpDomainEventDispatcher(),
            new FixedCurrentTenant(tenantId));
    }

    private sealed class TestMultiTenantDbContext(
        DbContextOptions<TestMultiTenantDbContext> options,
        TimeProvider timeProvider,
        ICurrentUser<Guid> currentUser,
        IAuditActorResolver<Guid, Guid?> auditActorResolver,
        IDomainEventDispatcher domainEventDispatcher,
        ICurrentTenant<Guid> currentTenant)
        : MultiTenantDbContext<Guid, Guid?, Guid>(
            options,
            timeProvider,
            currentUser,
            auditActorResolver,
            domainEventDispatcher,
            currentTenant)
    {
        public DbSet<TenantAggregate> Aggregates => Set<TenantAggregate>();

        protected override void ConfigureModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TenantAggregate>(builder =>
            {
                builder.HasKey(aggregate => aggregate.Id);
                builder.Ignore(aggregate => aggregate.DomainEvents);
            });
        }
    }

    private sealed class TenantAggregate : AggregateRoot<Guid>,
        IAuditable<Guid?>,
        IMultiTenant<Guid>,
        ISoftDelete<Guid?>
    {
        private TenantAggregate()
        {
        }

        public TenantAggregate(
            Guid id,
            Guid tenantId,
            bool isDeleted = false) : base(id)
        {
            TenantId = tenantId;
            IsDeleted = isDeleted;
        }

        public Guid TenantId { get; private set; }

        public void MoveToTenant(Guid tenantId) => TenantId = tenantId;
        public DateTimeOffset CreatedAt { get; private set; }
        public Guid? CreatedBy { get; private set; }
        public DateTimeOffset? UpdatedAt { get; private set; }
        public Guid? UpdatedBy { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTimeOffset? DeletedAt { get; private set; }
        public Guid? DeletedBy { get; private set; }
    }

    private sealed class FixedCurrentUser(Guid userId) : ICurrentUser<Guid>
    {
        public bool TryGetUserId(out Guid resolvedUserId)
        {
            resolvedUserId = userId;
            return true;
        }
    }

    private sealed class FixedCurrentTenant(Guid? tenantId)
        : ICurrentTenant<Guid>
    {
        public bool TryGetTenantId(out Guid resolvedTenantId)
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

    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
