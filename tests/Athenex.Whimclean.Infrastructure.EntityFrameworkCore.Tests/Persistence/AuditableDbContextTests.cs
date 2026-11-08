using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Application.Persistence;
using Athenex.Whimclean.Domain.Entities;
using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Infrastructure.Auditing;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Tests.Persistence;

public sealed class AuditableDbContextTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 21, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChangesAsync_StampsShadowCreateAuditAndDispatchesDomainEvents()
    {
        var actorId = Guid.NewGuid();
        var dispatcher = new RecordingDomainEventDispatcher();

        await using var context = CreateContext(actorId, Now, dispatcher);
        var aggregate = new TestAggregate(Guid.NewGuid());

        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        var entry = context.Entry(aggregate);
        Assert.Equal(Now, entry.Property(AuditStampNames.CreatedAt).CurrentValue);
        Assert.Equal(actorId, entry.Property(AuditStampNames.CreatedBy).CurrentValue);
        Assert.Null(entry.Property(AuditStampNames.UpdatedAt).CurrentValue);
        Assert.Null(entry.Property(AuditStampNames.UpdatedBy).CurrentValue);
        Assert.Single(dispatcher.Events);
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public async Task SaveChangesAsync_StampsDeclaredClrPropertyWhenEntityExposesIt()
    {
        var actorId = Guid.NewGuid();
        await using var context = CreateContext(actorId, Now, new RecordingDomainEventDispatcher());

        var entity = new SelfDescribedEntity(Guid.NewGuid());
        context.SelfDescribed.Add(entity);
        await context.SaveChangesAsync();

        Assert.Equal(Now, entity.CreatedAt);
        Assert.Equal(actorId, entity.CreatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_StampsUpdateAudit()
    {
        var creatorId = Guid.NewGuid();
        var updaterId = Guid.NewGuid();
        var updatedAt = Now.AddHours(1);
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();
        var aggregateId = Guid.NewGuid();

        await using (var createContext = CreateContext(
                         creatorId,
                         Now,
                         new RecordingDomainEventDispatcher(),
                         databaseRoot,
                         databaseName))
        {
            createContext.Aggregates.Add(new TestAggregate(aggregateId));
            await createContext.SaveChangesAsync();
        }

        await using var updateContext = CreateContext(
            updaterId,
            updatedAt,
            new RecordingDomainEventDispatcher(),
            databaseRoot,
            databaseName);
        var aggregate = await updateContext.Aggregates.SingleAsync();
        aggregate.Rename("changed");

        await updateContext.SaveChangesAsync();

        var entry = updateContext.Entry(aggregate);
        Assert.Equal(Now, entry.Property(AuditStampNames.CreatedAt).CurrentValue);
        Assert.Equal(creatorId, entry.Property(AuditStampNames.CreatedBy).CurrentValue);
        Assert.Equal(updatedAt, entry.Property(AuditStampNames.UpdatedAt).CurrentValue);
        Assert.Equal(updaterId, entry.Property(AuditStampNames.UpdatedBy).CurrentValue);
    }

    [Fact]
    public async Task Remove_SoftDeletesConfiguredEntityAndHidesItFromQueries()
    {
        var deleterId = Guid.NewGuid();
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();
        var entityId = Guid.NewGuid();

        await using (var seedContext = CreateContext(
                         Guid.NewGuid(),
                         Now.AddDays(-1),
                         new RecordingDomainEventDispatcher(),
                         databaseRoot,
                         databaseName))
        {
            seedContext.SoftDeletable.Add(new SoftDeletableEntity(entityId));
            await seedContext.SaveChangesAsync();
        }

        await using var deleteContext = CreateContext(
            deleterId,
            Now,
            new RecordingDomainEventDispatcher(),
            databaseRoot,
            databaseName);
        var entity = await deleteContext.SoftDeletable.SingleAsync();

        deleteContext.SoftDeletable.Remove(entity);
        await deleteContext.SaveChangesAsync();

        var entry = deleteContext.Entry(entity);
        Assert.Equal(EntityState.Unchanged, entry.State);
        Assert.True(deleteContext.IsSoftDeleted(entity));
        Assert.Equal(Now, entry.Property(SoftDeleteNames.DeletedAt).CurrentValue);
        Assert.Equal(deleterId, entry.Property(SoftDeleteNames.DeletedBy).CurrentValue);

        await using var queryContext = CreateContext(
            Guid.NewGuid(),
            Now,
            new RecordingDomainEventDispatcher(),
            databaseRoot,
            databaseName);
        Assert.Empty(await queryContext.SoftDeletable.ToArrayAsync());
        Assert.Single(await queryContext.SoftDeletable.IncludeSoftDeleted().ToArrayAsync());
    }

    [Fact]
    public async Task Restore_ClearsSoftDeleteStamps()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();

        await using (var seedContext = CreateContext(
                         Guid.NewGuid(),
                         Now,
                         new RecordingDomainEventDispatcher(),
                         databaseRoot,
                         databaseName))
        {
            var seeded = new SoftDeletableEntity(Guid.NewGuid());
            seedContext.SoftDeletable.Add(seeded);
            await seedContext.SaveChangesAsync();
            seedContext.SoftDeletable.Remove(seeded);
            await seedContext.SaveChangesAsync();
        }

        await using var restoreContext = CreateContext(
            Guid.NewGuid(),
            Now.AddHours(2),
            new RecordingDomainEventDispatcher(),
            databaseRoot,
            databaseName);
        var entity = await restoreContext.SoftDeletable.IncludeSoftDeleted().SingleAsync();

        restoreContext.Restore(entity);
        await restoreContext.SaveChangesAsync();

        var entry = restoreContext.Entry(entity);
        Assert.False(restoreContext.IsSoftDeleted(entity));
        Assert.Null(entry.Property(SoftDeleteNames.DeletedAt).CurrentValue);
        Assert.Null(entry.Property(SoftDeleteNames.DeletedBy).CurrentValue);
        Assert.Single(await restoreContext.SoftDeletable.ToArrayAsync());
    }

    [Fact]
    public async Task Add_AlreadyDeleted_StampsDeleteAuditForImports()
    {
        var importerId = Guid.NewGuid();
        await using var context = CreateContext(importerId, Now, new RecordingDomainEventDispatcher());

        var entity = new SoftDeletableEntity(Guid.NewGuid());
        context.SoftDeletable.Add(entity);
        context.Entry(entity).Property(SoftDeleteNames.IsDeleted).CurrentValue = true;
        await context.SaveChangesAsync();

        var entry = context.Entry(entity);
        Assert.True(context.IsSoftDeleted(entity));
        Assert.Equal(Now, entry.Property(SoftDeleteNames.DeletedAt).CurrentValue);
        Assert.Equal(importerId, entry.Property(SoftDeleteNames.DeletedBy).CurrentValue);
        Assert.Empty(await context.SoftDeletable.ToArrayAsync());
    }

    [Fact]
    public async Task Restore_RejectsEntityWithoutSoftDelete()
    {
        await using var context = CreateContext(
            Guid.NewGuid(),
            Now,
            new RecordingDomainEventDispatcher());
        var entity = new PlainEntity(Guid.NewGuid());
        context.PlainEntities.Add(entity);

        var exception = Assert.Throws<InvalidOperationException>(
            () => context.Restore(entity));

        Assert.Contains("HasSoftDelete", exception.Message);
    }

    [Fact]
    public async Task HardDelete_RemovesSoftDeletableRow()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();

        await using (var seedContext = CreateContext(
                         Guid.NewGuid(),
                         Now,
                         new RecordingDomainEventDispatcher(),
                         databaseRoot,
                         databaseName))
        {
            seedContext.SoftDeletable.Add(new SoftDeletableEntity(Guid.NewGuid()));
            await seedContext.SaveChangesAsync();
        }

        await using var purgeContext = CreateContext(
            Guid.NewGuid(),
            Now,
            new RecordingDomainEventDispatcher(),
            databaseRoot,
            databaseName);
        var entity = await purgeContext.SoftDeletable.SingleAsync();

        purgeContext.HardDelete(entity);
        await purgeContext.SaveChangesAsync();

        Assert.Empty(await purgeContext.SoftDeletable.IncludeSoftDeleted().ToArrayAsync());
    }

    [Fact]
    public async Task Remove_StillDeletesEntityWithoutSoftDelete()
    {
        await using var context = CreateContext(
            Guid.NewGuid(),
            Now,
            new RecordingDomainEventDispatcher());
        var entity = new PlainEntity(Guid.NewGuid());
        context.PlainEntities.Add(entity);
        await context.SaveChangesAsync();

        context.PlainEntities.Remove(entity);
        await context.SaveChangesAsync();

        Assert.Empty(await context.PlainEntities.ToArrayAsync());
    }

    [Fact]
    public void SaveChanges_ThrowsBecauseDomainEventDispatchIsAsync()
    {
        using var context = CreateContext(
            actorId: null,
            Now,
            new RecordingDomainEventDispatcher());

        var exception = Assert.Throws<InvalidOperationException>(
            () => context.SaveChanges());

        Assert.Contains("SaveChangesAsync", exception.Message);
    }

    [Fact]
    public void SaveChanges_BoolOverload_ThrowsBecauseDomainEventDispatchIsAsync()
    {
        using var context = CreateContext(
            actorId: null,
            Now,
            new RecordingDomainEventDispatcher());

        var exception = Assert.Throws<InvalidOperationException>(
            () => context.SaveChanges(acceptAllChangesOnSuccess: true));

        Assert.Contains("SaveChangesAsync", exception.Message);
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutCurrentUser_RejectsAnyWrite()
    {
        await using var context = CreateContext(
            actorId: null,
            Now,
            new RecordingDomainEventDispatcher());

        context.PlainEntities.Add(new PlainEntity(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<CurrentUserRequiredException>(
            () => context.SaveChangesAsync());

        Assert.Contains("current user", exception.Message);
    }

    [Fact]
    public async Task SaveChangesAsync_WrapsEfConcurrencyException()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();
        var entityId = Guid.NewGuid();

        await using (var seedContext = CreateContext(
                         Guid.NewGuid(),
                         Now,
                         new RecordingDomainEventDispatcher(),
                         databaseRoot,
                         databaseName))
        {
            seedContext.ConcurrentEntities.Add(new ConcurrentEntity(entityId));
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = CreateContext(
            Guid.NewGuid(),
            Now,
            new RecordingDomainEventDispatcher(),
            databaseRoot,
            databaseName);
        await using var secondContext = CreateContext(
            Guid.NewGuid(),
            Now,
            new RecordingDomainEventDispatcher(),
            databaseRoot,
            databaseName);

        var first = await firstContext.ConcurrentEntities.SingleAsync();
        var second = await secondContext.ConcurrentEntities.SingleAsync();
        first.Change();
        second.Change();

        await firstContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<PersistenceConcurrencyException>(
            () => secondContext.SaveChangesAsync());

        Assert.IsType<DbUpdateConcurrencyException>(exception.InnerException);
    }

    [Fact]
    public async Task SaveChangesAsync_PreservesEventsRaisedDuringDispatch()
    {
        var dispatcher = new FollowUpRaisingDispatcher();
        await using var context = CreateContext(
            Guid.NewGuid(),
            Now,
            dispatcher);

        var aggregate = new TestAggregate(Guid.NewGuid());
        dispatcher.Aggregate = aggregate;
        context.Aggregates.Add(aggregate);

        await context.SaveChangesAsync();

        // The event a handler raised during dispatch must survive on the
        // aggregate for the next commit instead of being cleared unseen.
        Assert.Single(aggregate.DomainEvents);
    }

    [Fact]
    public async Task SaveChangesAsync_WrapsDispatchFailureAfterCommit()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();

        await using (var context = CreateContext(
                         Guid.NewGuid(),
                         Now,
                         new ThrowingDomainEventDispatcher(),
                         databaseRoot,
                         databaseName))
        {
            var aggregateId = Guid.NewGuid();
            context.Aggregates.Add(new TestAggregate(aggregateId));

            var exception =
                await Assert.ThrowsAsync<DomainEventDispatchException>(
                    () => context.SaveChangesAsync());

            // The aggregate was cleared before dispatch, so the exception
            // must carry the only remaining copy of the undelivered event.
            var undeliveredEvent = Assert.Single(exception.UndeliveredEvents);
            Assert.Equal(
                aggregateId,
                Assert.IsType<TestDomainEvent>(undeliveredEvent).Id);
        }

        await using var verificationContext = CreateContext(
            Guid.NewGuid(),
            Now,
            new RecordingDomainEventDispatcher(),
            databaseRoot,
            databaseName);

        Assert.Equal(1, await verificationContext.Aggregates.CountAsync());
    }

    private static TestDbContext CreateContext(
        Guid? actorId,
        DateTimeOffset now,
        IDomainEventDispatcher dispatcher,
        InMemoryDatabaseRoot? databaseRoot = null,
        string? databaseName = null)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder.UseInMemoryDatabase(
            databaseName ?? Guid.NewGuid().ToString(),
            databaseRoot ?? new InMemoryDatabaseRoot());

        return new TestDbContext(
            optionsBuilder.Options,
            new FixedTimeProvider(now),
            new FixedCurrentUser(actorId),
            new NullableAuditActorResolver<Guid>(),
            dispatcher);
    }

    private sealed class TestDbContext(
        DbContextOptions<TestDbContext> options,
        TimeProvider timeProvider,
        ICurrentUser<Guid> currentUser,
        IAuditActorResolver<Guid, Guid?> auditActorResolver,
        IDomainEventDispatcher domainEventDispatcher)
        : AuditableDbContext<Guid, Guid?>(
            options,
            timeProvider,
            currentUser,
            auditActorResolver,
            domainEventDispatcher)
    {
        public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();
        public DbSet<PlainEntity> PlainEntities => Set<PlainEntity>();
        public DbSet<SoftDeletableEntity> SoftDeletable => Set<SoftDeletableEntity>();
        public DbSet<SelfDescribedEntity> SelfDescribed => Set<SelfDescribedEntity>();
        public DbSet<ConcurrentEntity> ConcurrentEntities => Set<ConcurrentEntity>();

        protected override void ConfigureModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAggregate>(builder =>
            {
                builder.HasKey(aggregate => aggregate.Id);
                builder.Ignore(aggregate => aggregate.DomainEvents);
                builder.HasAuditStamps();
            });

            modelBuilder.Entity<PlainEntity>()
                .HasKey(entity => entity.Id);

            modelBuilder.Entity<SoftDeletableEntity>(builder =>
            {
                builder.HasKey(entity => entity.Id);
                builder.HasSoftDelete();
            });

            modelBuilder.Entity<SelfDescribedEntity>(builder =>
            {
                builder.HasKey(entity => entity.Id);
                builder.HasAuditStamps();
            });

            modelBuilder.Entity<ConcurrentEntity>(builder =>
            {
                builder.HasKey(entity => entity.Id);
                builder.Property(entity => entity.Version)
                    .IsConcurrencyToken();
            });
        }
    }

    private sealed class PlainEntity(Guid id)
    {
        public Guid Id { get; } = id;
    }

    private sealed class SoftDeletableEntity(Guid id)
    {
        public Guid Id { get; } = id;
    }

    /// <summary>
    /// An entity that exposes CreatedAt/CreatedBy itself; the context
    /// stamps those CLR properties instead of adding shadow ones.
    /// </summary>
    private sealed class SelfDescribedEntity(Guid id)
    {
        public Guid Id { get; } = id;
        public DateTimeOffset CreatedAt { get; private set; }
        public Guid? CreatedBy { get; private set; }
    }

    private sealed class ConcurrentEntity(Guid id)
    {
        public Guid Id { get; } = id;
        public int Version { get; private set; } = 1;

        public void Change() => Version++;
    }

    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        private TestAggregate()
        {
        }

        public TestAggregate(Guid id) : base(id)
        {
            RaiseDomainEvent(new TestDomainEvent(id));
        }

        public string Name { get; private set; } = "initial";

        public void Rename(string name) => Name = name;

        public void RaiseFollowUpEvent() =>
            RaiseDomainEvent(new TestDomainEvent(Id));
    }

    private sealed record TestDomainEvent(Guid Id) : DomainEvent;

    private sealed class FixedCurrentUser(Guid? userId)
        : ICurrentUser<Guid>
    {
        public bool TryGetUserId(out Guid resolvedUserId)
        {
            if (userId is Guid value)
            {
                resolvedUserId = value;
                return true;
            }

            resolvedUserId = default;
            return false;
        }
    }

    private sealed class RecordingDomainEventDispatcher
        : IDomainEventDispatcher
    {
        public List<IDomainEvent> Events { get; } = [];

        public Task DispatchAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken = default)
        {
            Events.Add(domainEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FollowUpRaisingDispatcher : IDomainEventDispatcher
    {
        private bool _hasRaisedFollowUp;

        public TestAggregate? Aggregate { get; set; }

        public Task DispatchAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken = default)
        {
            if (!_hasRaisedFollowUp)
            {
                _hasRaisedFollowUp = true;
                Aggregate?.RaiseFollowUpEvent();
            }

            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Handler failed.");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
