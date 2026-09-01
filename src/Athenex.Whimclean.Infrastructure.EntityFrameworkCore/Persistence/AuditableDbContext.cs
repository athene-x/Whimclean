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

/// <summary>
/// DbContext base that turns every save into an audited unit of work: it
/// requires an authenticated current user for any change, stamps
/// Created/Updated/Deleted audit fields from <see cref="TimeProvider" />
/// and <see cref="IAuditActorResolver{TUserId, TActor}" />, rejects hard
/// deletes of soft-deletable entities, wraps concurrency failures in
/// <see cref="PersistenceConcurrencyException" />, and dispatches queued
/// domain events once the commit has succeeded. It also applies the named
/// soft-delete query filter
/// (<see cref="WhimcleanQueryFilters.SoftDelete" />) to root
/// <see cref="ISoftDelete{TActor}" /> entity types, so deleted rows are
/// hidden from queries by default.
/// </summary>
/// <typeparam name="TUserId">
/// Identifier type reported by <see cref="ICurrentUser{TUserId}" />.
/// </typeparam>
/// <typeparam name="TActor">
/// Value stored in the audit actor columns, produced by the resolver.
/// </typeparam>
/// <param name="options">EF Core options for the concrete context.</param>
/// <param name="timeProvider">
/// Clock supplying the UTC audit timestamps.
/// </param>
/// <param name="currentUser">
/// Source of the user every write is attributed to.
/// </param>
/// <param name="auditActorResolver">
/// Maps the current user id to the stored actor value.
/// </param>
/// <param name="domainEventDispatcher">
/// Receives the queued domain events after a successful save.
/// </param>
public abstract class AuditableDbContext<TUserId, TActor>(
    DbContextOptions options,
    TimeProvider timeProvider,
    ICurrentUser<TUserId> currentUser,
    IAuditActorResolver<TUserId, TActor> auditActorResolver,
    IDomainEventDispatcher domainEventDispatcher)
    : DbContext(options), IUnitOfWork
    where TUserId : notnull
{
    /// <summary>
    /// Persists all pending changes and then dispatches the queued domain
    /// events; equivalent to
    /// <see cref="SaveChangesAsync(CancellationToken)" />.
    /// </summary>
    public Task CommitAsync(
        CancellationToken cancellationToken = default) =>
        SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    /// <summary>
    /// Not supported: domain event dispatch is asynchronous, so
    /// synchronous saves are rejected by design.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    public override int SaveChanges() =>
        throw CreateSynchronousSaveException();

    /// <summary>
    /// Not supported: domain event dispatch is asynchronous, so
    /// synchronous saves are rejected by design.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw CreateSynchronousSaveException();

    /// <summary>
    /// Runs the audited save pipeline with
    /// <c>acceptAllChangesOnSuccess</c> set to <see langword="true" />;
    /// see <see cref="SaveChangesAsync(bool, CancellationToken)" /> for
    /// the pipeline and its exceptions.
    /// </summary>
    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default) =>
        SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    /// <summary>
    /// Runs the audited save pipeline: snapshots the queued domain events,
    /// requires a current user when anything changed, rejects hard deletes
    /// of soft-deletable entities, runs the infrastructure and application
    /// write-validation hooks, stamps the audit fields, saves, clears the
    /// events, then dispatches them with
    /// <see cref="CancellationToken.None" /> — post-commit work must not
    /// be cancelled.
    /// </summary>
    /// <returns>The number of state entries written to the store.</returns>
    /// <exception cref="CurrentUserRequiredException">
    /// Changes are pending but no current user is available.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A soft-deletable entity was hard-removed, which would bypass the
    /// audit trail.
    /// </exception>
    /// <exception cref="PersistenceConcurrencyException">
    /// A record changed after it was loaded; reload it and retry.
    /// </exception>
    /// <exception cref="DomainEventDispatchException">
    /// The save succeeded but an event handler failed; do not retry the
    /// save.
    /// </exception>
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

        RejectHardDeletesOfSoftDeletableEntities(changedEntries);
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

        // Clear before dispatching so events raised by handlers during
        // dispatch survive on the aggregate for the next commit.
        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        try
        {
            // The commit already succeeded; a caller-requested cancellation
            // must not abort delivery of events for persisted changes.
            foreach (var domainEvent in domainEvents)
            {
                await domainEventDispatcher.DispatchAsync(
                    domainEvent,
                    CancellationToken.None);
            }
        }
        catch (Exception exception)
        {
            throw new DomainEventDispatchException(exception);
        }

        return savedCount;
    }

    /// <summary>
    /// Sealed to fix the composition order: consumer configuration via
    /// <see cref="ConfigureModel" /> first, then the soft-delete query
    /// filters, then
    /// <see cref="ConfigureAdditionalInfrastructureModel" />.
    /// </summary>
    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureModel(modelBuilder);
        ApplySoftDeleteQueryFilters(modelBuilder);
        ConfigureAdditionalInfrastructureModel(modelBuilder);
    }

    /// <summary>
    /// Extension point for derived infrastructure contexts to add model
    /// configuration after the soft-delete filters are applied (the
    /// multitenancy context installs its tenant filters here). A no-op by
    /// default; consumer models belong in <see cref="ConfigureModel" />
    /// instead.
    /// </summary>
    protected virtual void ConfigureAdditionalInfrastructureModel(
        ModelBuilder modelBuilder)
    {
    }

    /// <summary>
    /// Consumer hook for entity model configuration, invoked before the
    /// infrastructure query filters are applied. A no-op by default.
    /// </summary>
    protected virtual void ConfigureModel(ModelBuilder modelBuilder)
    {
    }

    /// <summary>
    /// Infrastructure hook that inspects the tracked changes before audit
    /// stamping and save; throw to abort the commit (the multitenancy
    /// context enforces tenant ownership here). A no-op by default.
    /// </summary>
    protected virtual void ValidateInfrastructureWrites()
    {
    }

    /// <summary>
    /// Application-level hook that inspects the tracked changes before
    /// audit stamping and save; throw to abort the commit. A no-op by
    /// default.
    /// </summary>
    protected virtual void ValidateApplicationWrites()
    {
    }

    private static InvalidOperationException CreateSynchronousSaveException() =>
        new(
            "Synchronous persistence is not supported. " +
            "Use SaveChangesAsync or IUnitOfWork.CommitAsync so domain events " +
            "can be dispatched asynchronously.");

    private static void RejectHardDeletesOfSoftDeletableEntities(
        IReadOnlyCollection<EntityEntry> entries)
    {
        var hardDeletedEntry = entries.FirstOrDefault(entry =>
            entry.State == EntityState.Deleted &&
            entry.Entity is ISoftDelete<TActor>);

        if (hardDeletedEntry is not null)
        {
            throw new InvalidOperationException(
                $"'{hardDeletedEntry.Entity.GetType().Name}' supports soft " +
                "deletion. Removing it would bypass the soft-delete audit " +
                "trail; use the entity's soft-delete operation instead of " +
                "Remove.");
        }
    }

    private TUserId RequireCurrentUserForWrites(bool hasChanges)
    {
        if (!hasChanges)
        {
            return default!;
        }

        return currentUser.GetRequiredUserId();
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
                WhimcleanQueryFilters.SoftDelete,
                Expression.Lambda(filterBody, parameter));
        }
    }
}
