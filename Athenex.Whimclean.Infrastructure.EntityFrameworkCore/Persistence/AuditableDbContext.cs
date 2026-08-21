using System.Linq.Expressions;
using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Application.Persistence;
using Athenex.Whimclean.Domain.Auditables;
using Athenex.Whimclean.Domain.Behaviors;
using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Infrastructure.Auditing;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence;

public abstract class AuditableDbContext<TUserId, TActor>(
    DbContextOptions options,
    TimeProvider timeProvider,
    ICurrentUser<TUserId> currentUser,
    IAuditActorResolver<TUserId, TActor> auditActorResolver,
    IDomainEventDispatcher domainEventDispatcher)
    : DbContext(options), IUnitOfWork
    where TUserId : notnull
{
    public Task CommitAsync(
        CancellationToken cancellationToken = default) =>
        SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    public override int SaveChanges() =>
        throw CreateSynchronousSaveException();

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw CreateSynchronousSaveException();

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default) =>
        SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToArray();

        var domainEvents = aggregates
            .SelectMany(entity => entity.DomainEvents)
            .ToArray();

        var changedEntries = ChangeTracker
            .Entries()
            .Where(entry => entry.State is EntityState.Added or
                EntityState.Modified or EntityState.Deleted)
            .ToArray();

        var userId = RequireCurrentUserForWrites(changedEntries.Length > 0);

        ValidateInfrastructureWrites();
        ValidateApplicationWrites();
        ApplyAuditFields(changedEntries, userId);

        int savedCount;

        try
        {
            savedCount = await base.SaveChangesAsync(
                acceptAllChangesOnSuccess,
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new PersistenceConcurrencyException(
                "The record changed after it was loaded. Reload it and try again.",
                exception);
        }

        foreach (var domainEvent in domainEvents)
        {
            await domainEventDispatcher.DispatchAsync(
                domainEvent,
                cancellationToken);
        }

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        return savedCount;
    }

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureModel(modelBuilder);
        ApplySoftDeleteQueryFilters(modelBuilder);
        ConfigureAdditionalInfrastructureModel(modelBuilder);
    }

    protected virtual void ConfigureAdditionalInfrastructureModel(
        ModelBuilder modelBuilder)
    {
    }

    protected virtual void ConfigureModel(ModelBuilder modelBuilder)
    {
    }

    protected virtual void ValidateInfrastructureWrites()
    {
    }

    protected virtual void ValidateApplicationWrites()
    {
    }

    private static InvalidOperationException CreateSynchronousSaveException() =>
        new(
            "Synchronous persistence is not supported. " +
            "Use SaveChangesAsync or IUnitOfWork.CommitAsync so domain events " +
            "can be dispatched asynchronously.");

    private TUserId RequireCurrentUserForWrites(bool hasChanges)
    {
        if (!hasChanges)
        {
            return default!;
        }

        if (!currentUser.TryGetUserId(out var userId))
        {
            throw new CurrentUserRequiredException();
        }

        return userId;
    }

    private void ApplyAuditFields(
        IReadOnlyCollection<EntityEntry> entries,
        TUserId userId)
    {
        var requiresActor = entries.Any(entry =>
            (entry.State == EntityState.Added &&
             entry.Entity is ICreateAuditable<TActor>) ||
            (entry.State == EntityState.Modified &&
             entry.Entity is IUpdateAuditable<TActor>) ||
            (entry.State == EntityState.Modified &&
             entry.Entity is ISoftDelete<TActor> &&
             entry.Property(nameof(ISoftDelete<TActor>.IsDeleted)).IsModified));

        if (!requiresActor)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var actor = auditActorResolver.Resolve(userId);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added &&
                entry.Entity is ICreateAuditable<TActor>)
            {
                entry.Property(nameof(ICreateAuditable<>.CreatedAt))
                    .CurrentValue = now;
                entry.Property(nameof(ICreateAuditable<TActor>.CreatedBy))
                    .CurrentValue = actor;
            }

            if (entry.State == EntityState.Modified &&
                entry.Entity is IUpdateAuditable<TActor>)
            {
                entry.Property(nameof(IUpdateAuditable<>.UpdatedAt))
                    .CurrentValue = now;
                entry.Property(nameof(IUpdateAuditable<TActor>.UpdatedBy))
                    .CurrentValue = actor;
            }

            if (entry.State == EntityState.Modified &&
                entry.Entity is ISoftDelete<TActor> &&
                entry.Property(nameof(ISoftDelete<TActor>.IsDeleted))
                    .IsModified)
            {
                var isDeleted = (bool)entry
                    .Property(nameof(ISoftDelete<TActor>.IsDeleted))
                    .CurrentValue!;

                entry.Property(nameof(ISoftDelete<>.DeletedAt))
                    .CurrentValue = isDeleted ? now : null;
                entry.Property(nameof(ISoftDelete<TActor>.DeletedBy))
                    .CurrentValue = isDeleted ? actor : default;
            }
        }
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        var softDeleteType = typeof(ISoftDelete<TActor>);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!softDeleteType.IsAssignableFrom(entityType.ClrType) ||
                (entityType.BaseType is not null &&
                 softDeleteType.IsAssignableFrom(entityType.BaseType.ClrType)))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isDeleted = Expression.Property(
                parameter,
                nameof(ISoftDelete<TActor>.IsDeleted));
            var filterBody = Expression.Not(isDeleted);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                "SoftDeleteFilter",
                Expression.Lambda(filterBody, parameter));
        }
    }
}
