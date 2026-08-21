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
            .IgnoreQueryFilters()
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

    private static TestDbContext CreateContext(
        Guid? actorId,
        DateTimeOffset now,
        RecordingDomainEventDispatcher dispatcher,
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

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
