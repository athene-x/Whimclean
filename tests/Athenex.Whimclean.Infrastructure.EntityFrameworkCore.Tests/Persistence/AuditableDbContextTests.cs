using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Application.Persistence;
using Athenex.Whimclean.Domain.Auditables;
using Athenex.Whimclean.Domain.Behaviors;
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
    [Fact]
    public async Task SaveChangesAsync_AppliesAuditAndDispatchesDomainEvents()
    {
        var actorId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 8, 21, 10, 0, 0, TimeSpan.Zero);
        var dispatcher = new RecordingDomainEventDispatcher();

        await using var context = CreateContext(actorId, now, dispatcher);
        var aggregate = new TestAggregate(Guid.NewGuid());

        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        Assert.Equal(now, aggregate.CreatedAt);
        Assert.Equal(actorId, aggregate.CreatedBy);
        Assert.Single(dispatcher.Events);
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void SaveChanges_ThrowsBecauseDomainEventDispatchIsAsync()
    {
        using var context = CreateContext(
            actorId: null,
            DateTimeOffset.UtcNow,
            new RecordingDomainEventDispatcher());

        var exception = Assert.Throws<InvalidOperationException>(
            () => context.SaveChanges());

        Assert.Contains("SaveChangesAsync", exception.Message);
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutCurrentUser_RejectsAnyWrite()
    {
        await using var context = CreateContext(
            actorId: null,
            DateTimeOffset.UtcNow,
            new RecordingDomainEventDispatcher());

        context.PlainEntities.Add(new PlainEntity(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<CurrentUserRequiredException>(
            () => context.SaveChangesAsync());

        Assert.Contains("current user", exception.Message);
    }

    [Fact]
    public async Task QueryFilter_ExcludesSoftDeletedEntityConfiguredByApplication()
    {
        var actorId = Guid.NewGuid();
        await using var context = CreateContext(
            actorId,
            DateTimeOffset.UtcNow,
            new RecordingDomainEventDispatcher());

        context.Set<SoftDeleteEntity>().AddRange(
            new SoftDeleteEntity(Guid.NewGuid(), isDeleted: false),
            new SoftDeleteEntity(Guid.NewGuid(), isDeleted: true));
        await context.SaveChangesAsync();

        var visible = await context.Set<SoftDeleteEntity>().ToArrayAsync();
        var all = await context.Set<SoftDeleteEntity>()
            .IncludeSoftDeleted()
            .ToArrayAsync();

        Assert.Single(visible);
        Assert.Equal(2, all.Length);
    }

    [Fact]
    public async Task SaveChangesAsync_WrapsEfConcurrencyException()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();
        var entityId = Guid.NewGuid();

        await using (var seedContext = CreateContext(
                         Guid.NewGuid(),
                         DateTimeOffset.UtcNow,
                         new RecordingDomainEventDispatcher(),
                         databaseRoot,
                         databaseName))
        {
            seedContext.ConcurrentEntities.Add(new ConcurrentEntity(entityId));
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = CreateContext(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            new RecordingDomainEventDispatcher(),
            databaseRoot,
            databaseName);
        await using var secondContext = CreateContext(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
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
    public async Task SaveChangesAsync_RejectsHardDeleteOfSoftDeletableEntity()
    {
        await using var context = CreateContext(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            new RecordingDomainEventDispatcher());

        var entity = new SoftDeleteEntity(Guid.NewGuid(), isDeleted: false);
        context.Set<SoftDeleteEntity>().Add(entity);
        await context.SaveChangesAsync();

        context.Set<SoftDeleteEntity>().Remove(entity);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());

        Assert.Contains("soft-delete", exception.Message);
    }

    [Fact]
    public void SaveChanges_BoolOverload_ThrowsBecauseDomainEventDispatchIsAsync()
    {
        using var context = CreateContext(
            actorId: null,
            DateTimeOffset.UtcNow,
            new RecordingDomainEventDispatcher());

        var exception = Assert.Throws<InvalidOperationException>(
            () => context.SaveChanges(acceptAllChangesOnSuccess: true));

        Assert.Contains("SaveChangesAsync", exception.Message);
    }

    [Fact]
    public async Task SaveChangesAsync_StampsUpdateAudit()
    {
        var creatorId = Guid.NewGuid();
        var updaterId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 8, 21, 10, 0, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddHours(1);
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();
        var aggregateId = Guid.NewGuid();

        await using (var createContext = CreateContext(
                         creatorId,
                         createdAt,
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
        updateContext.Entry(aggregate).State = EntityState.Modified;

        await updateContext.SaveChangesAsync();

        Assert.Equal(createdAt, aggregate.CreatedAt);
        Assert.Equal(creatorId, aggregate.CreatedBy);
        Assert.Equal(updatedAt, aggregate.UpdatedAt);
        Assert.Equal(updaterId, aggregate.UpdatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_StampsSoftDeleteAndClearsOnRestore()
    {
        var deleterId = Guid.NewGuid();
        var deletedAt = new DateTimeOffset(2026, 8, 21, 10, 0, 0, TimeSpan.Zero);
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();

        await using (var seedContext = CreateContext(
                         Guid.NewGuid(),
                         deletedAt.AddDays(-1),
                         new RecordingDomainEventDispatcher(),
                         databaseRoot,
                         databaseName))
        {
            seedContext.Set<SoftDeleteEntity>().Add(
                new SoftDeleteEntity(Guid.NewGuid(), isDeleted: false));
            await seedContext.SaveChangesAsync();
        }

        await using var deleteContext = CreateContext(
            deleterId,
            deletedAt,
            new RecordingDomainEventDispatcher(),
            databaseRoot,
            databaseName);
        var entity = await deleteContext.Set<SoftDeleteEntity>().SingleAsync();
        entity.Delete();

        await deleteContext.SaveChangesAsync();

        Assert.Equal(deletedAt, entity.DeletedAt);
        Assert.Equal(deleterId, entity.DeletedBy);

        entity.Restore();

        await deleteContext.SaveChangesAsync();

        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.DeletedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_PreservesEventsRaisedDuringDispatch()
    {
        var dispatcher = new FollowUpRaisingDispatcher();
        await using var context = CreateContext(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
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
                         DateTimeOffset.UtcNow,
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
            DateTimeOffset.UtcNow,
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
        public DbSet<ConcurrentEntity> ConcurrentEntities => Set<ConcurrentEntity>();

        protected override void ConfigureModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAggregate>(builder =>
            {
                builder.HasKey(aggregate => aggregate.Id);
                builder.Ignore(aggregate => aggregate.DomainEvents);
            });

            modelBuilder.Entity<PlainEntity>()
                .HasKey(entity => entity.Id);
            modelBuilder.Entity<SoftDeleteEntity>()
                .HasKey(entity => entity.Id);
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

    private sealed class SoftDeleteEntity : ISoftDelete<Guid?>
    {
        private SoftDeleteEntity()
        {
        }

        public SoftDeleteEntity(Guid id, bool isDeleted)
        {
            Id = id;
            IsDeleted = isDeleted;
        }

        public Guid Id { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTimeOffset? DeletedAt { get; private set; }
        public Guid? DeletedBy { get; private set; }

        public void Delete() => IsDeleted = true;

        public void Restore() => IsDeleted = false;
    }

    private sealed class ConcurrentEntity(Guid id)
    {
        public Guid Id { get; } = id;
        public int Version { get; private set; } = 1;

        public void Change() => Version++;
    }

    private sealed class TestAggregate : AggregateRoot<Guid>, IAuditable<Guid?>
    {
        private TestAggregate()
        {
        }

        public TestAggregate(Guid id) : base(id)
        {
            RaiseDomainEvent(new TestDomainEvent(id));
        }

        public DateTimeOffset CreatedAt { get; private set; }
        public Guid? CreatedBy { get; private set; }
        public DateTimeOffset? UpdatedAt { get; private set; }
        public Guid? UpdatedBy { get; private set; }

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
