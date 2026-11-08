using System.Linq.Expressions;
using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Application.Persistence;
using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Infrastructure.Auditing;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence;

/// <summary>
/// DbContext base that turns every save into an audited unit of work: it
/// requires an authenticated current user for any change, stamps the
/// audit properties of entity types configured with
/// <see cref="EntityTypeBuilderExtensions.HasAuditStamps(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder)" />
/// from <see cref="TimeProvider" /> and
/// <see cref="IAuditActorResolver{TUserId, TActor}" />, turns
/// <c>Remove</c> of entity types configured with
/// <see cref="EntityTypeBuilderExtensions.HasSoftDelete(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder)" />
/// into a soft delete hidden by the named query filter
/// <see cref="QueryFilterNames.SoftDelete" />, wraps concurrency failures
/// in <see cref="PersistenceConcurrencyException" />, and dispatches
/// queued domain events once the commit has succeeded. The audit and
/// soft-delete properties are shadow properties (see
/// <see cref="AuditStampNames" /> and <see cref="SoftDeleteNames" />), so
/// the domain model carries no persistence concerns.
/// </summary>
/// <typeparam name="TUserId">
/// Identifier type reported by <see cref="ICurrentUser{TUserId}" />.
/// </typeparam>
/// <typeparam name="TActor">
/// Value stored in the actor columns, produced by the resolver.
/// <c>CreatedBy</c> is required when this is a non-nullable value type
/// and nullable otherwise (including every reference type, whose nullable
/// annotation is not visible at runtime); the update and delete actors
/// are always nullable.
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
    private readonly HashSet<object> _hardDeletes =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Persists all pending changes and then dispatches the queued domain
    /// events; equivalent to
    /// <see cref="SaveChangesAsync(CancellationToken)" />.
    /// </summary>
    public Task CommitAsync(
        CancellationToken cancellationToken = default) =>
        SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    /// <summary>
    /// Deletes the row of a soft-deletable entity for real instead of
    /// soft-deleting it. For entity types without soft deletion this is
    /// the same as <see cref="DbContext.Remove(object)" />. Reserve it for
    /// explicitly authorized operations such as data purges.
    /// </summary>
    /// <param name="entity">The tracked or detached entity to delete.</param>
    public void HardDelete(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _hardDeletes.Add(entity);
        Remove(entity);
    }

    /// <summary>
    /// Reverses a soft delete: clears <c>IsDeleted</c> so the next save
    /// also clears <c>DeletedAt</c> and <c>DeletedBy</c>. Load the entity
    /// with <see cref="SoftDeleteQueryableExtensions.IncludeSoftDeleted{T}" />
    /// first, because the query filter hides deleted rows.
    /// </summary>
    /// <param name="entity">A tracked soft-deletable entity.</param>
    /// <exception cref="InvalidOperationException">
    /// The entity type is not configured with <c>HasSoftDelete</c>.
    /// </exception>
    public void Restore(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var entry = Entry(entity);

        if (!HasAnnotation(entry.Metadata, PersistenceAnnotations.SoftDelete))
        {
            throw new InvalidOperationException(
                $"'{entry.Metadata.ClrType.Name}' is not configured for soft " +
                "deletion; call HasSoftDelete() on its entity type to restore it.");
        }

        entry.Property(SoftDeleteNames.IsDeleted).CurrentValue = false;
    }

    /// <summary>
    /// Whether a tracked soft-deletable entity is currently marked as
    /// deleted. Returns <see langword="false" /> for entity types without
    /// soft deletion.
    /// </summary>
    public bool IsSoftDeleted(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var entry = Entry(entity);

        return HasAnnotation(entry.Metadata, PersistenceAnnotations.SoftDelete) &&
               entry.Property(SoftDeleteNames.IsDeleted).CurrentValue is true;
    }

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
    /// requires a current user when anything changed, converts removals
    /// of soft-deletable entities into soft deletes, runs the
    /// infrastructure and application write-validation hooks, stamps the
    /// audit and soft-delete properties, saves, clears the events, then
    /// dispatches them with <see cref="CancellationToken.None" /> —
    /// post-commit work must not be cancelled.
    /// </summary>
    /// <returns>The number of state entries written to the store.</returns>
    /// <exception cref="CurrentUserRequiredException">
    /// Changes are pending but no current user is available.
    /// </exception>
    /// <exception cref="PersistenceConcurrencyException">
    /// A record changed after it was loaded; reload it and retry.
    /// </exception>
    /// <exception cref="DomainEventDispatchException">
    /// The save succeeded but an event handler failed; do not retry the
    /// save. The events that never reached their handlers are on
    /// <see cref="DomainEventDispatchException.UndeliveredEvents" />.
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

        ConvertRemovalsToSoftDeletes(changedEntries);
        ValidateInfrastructureWrites();
        ValidateApplicationWrites();
        ApplyStamps(changedEntries, userId);

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

        _hardDeletes.Clear();

        // Clear before dispatching so events raised by handlers during
        // dispatch survive on the aggregate for the next commit.
        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        for (var index = 0; index < domainEvents.Length; index++)
        {
            try
            {
                // The commit already succeeded; a caller-requested
                // cancellation must not abort delivery of events for
                // persisted changes.
                await domainEventDispatcher.DispatchAsync(
                    domainEvents[index],
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                throw new DomainEventDispatchException(
                    domainEvents[index..],
                    exception);
            }
        }

        return savedCount;
    }

    /// <summary>
    /// Sealed to fix the composition order: consumer configuration via
    /// <see cref="ConfigureModel" /> first, then the audit-stamp and
    /// soft-delete properties with the soft-delete query filters, then
    /// <see cref="ConfigureAdditionalInfrastructureModel" />.
    /// </summary>
    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureModel(modelBuilder);
        ApplyAuditStampProperties(modelBuilder);
        ApplySoftDeleteProperties(modelBuilder);
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
    /// infrastructure properties and query filters are applied. Opt entity
    /// types into stamping and soft deletion here with
    /// <c>HasAuditStamps()</c> and <c>HasSoftDelete()</c>. A no-op by
    /// default.
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

    private static bool HasAnnotation(IReadOnlyEntityType entityType, string name)
    {
        for (var type = entityType; type is not null; type = type.BaseType)
        {
            if (type.FindAnnotation(name) is not null)
            {
                return true;
            }
        }

        return false;
    }

    private static bool DeclaresAnnotation(IReadOnlyEntityType entityType, string name) =>
        entityType.FindAnnotation(name) is not null &&
        (entityType.BaseType is null ||
         !HasAnnotation(entityType.BaseType, name));

    private static Type MakeNullable(Type type) =>
        type.IsValueType && Nullable.GetUnderlyingType(type) is null
            ? typeof(Nullable<>).MakeGenericType(type)
            : type;

    private static void AddProperty(
        ModelBuilder modelBuilder,
        Type entityClrType,
        string name,
        Type propertyType,
        bool required)
    {
        var builder = modelBuilder.Entity(entityClrType).Property(propertyType, name);

        if (!required)
        {
            builder.IsRequired(false);
        }
    }

    private static void ApplyAuditStampProperties(ModelBuilder modelBuilder)
    {
        var actorType = typeof(TActor);
        var nullableActorType = MakeNullable(actorType);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() ||
                !DeclaresAnnotation(entityType, PersistenceAnnotations.AuditStamps))
            {
                continue;
            }

            var clrType = entityType.ClrType;

            AddProperty(modelBuilder, clrType, AuditStampNames.CreatedAt, typeof(DateTimeOffset), required: true);
            AddProperty(modelBuilder, clrType, AuditStampNames.CreatedBy, actorType, required: actorType.IsValueType && nullableActorType != actorType);
            AddProperty(modelBuilder, clrType, AuditStampNames.UpdatedAt, typeof(DateTimeOffset?), required: false);
            AddProperty(modelBuilder, clrType, AuditStampNames.UpdatedBy, nullableActorType, required: false);
        }
    }

    private static void ApplySoftDeleteProperties(ModelBuilder modelBuilder)
    {
        var nullableActorType = MakeNullable(typeof(TActor));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Owned types cannot carry query filters; configuring one via
            // modelBuilder.Entity would also detach it from its owner.
            if (entityType.IsOwned() ||
                !DeclaresAnnotation(entityType, PersistenceAnnotations.SoftDelete))
            {
                continue;
            }

            var clrType = entityType.ClrType;

            AddProperty(modelBuilder, clrType, SoftDeleteNames.IsDeleted, typeof(bool), required: true);
            AddProperty(modelBuilder, clrType, SoftDeleteNames.DeletedAt, typeof(DateTimeOffset?), required: false);
            AddProperty(modelBuilder, clrType, SoftDeleteNames.DeletedBy, nullableActorType, required: false);

            var parameter = Expression.Parameter(clrType, "entity");
            var isDeleted = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                [typeof(bool)],
                parameter,
                Expression.Constant(SoftDeleteNames.IsDeleted));
            var filterBody = Expression.Not(isDeleted);

            modelBuilder.Entity(clrType).HasQueryFilter(
                QueryFilterNames.SoftDelete,
                Expression.Lambda(filterBody, parameter));
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

    private void ConvertRemovalsToSoftDeletes(IReadOnlyCollection<EntityEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (entry.State != EntityState.Deleted ||
                _hardDeletes.Contains(entry.Entity) ||
                !HasAnnotation(entry.Metadata, PersistenceAnnotations.SoftDelete))
            {
                continue;
            }

            // Go through Unchanged rather than Modified so that only the
            // soft-delete flag (and the stamps written below) count as
            // modified; marking every property would, for example, trip
            // the multitenancy check on an unchanged TenantId.
            entry.State = EntityState.Unchanged;
            entry.Property(SoftDeleteNames.IsDeleted).CurrentValue = true;
        }
    }

    private void ApplyStamps(IReadOnlyCollection<EntityEntry> entries, TUserId userId)
    {
        var stampedEntries = entries
            .Where(entry =>
                (entry.State is EntityState.Added or EntityState.Modified &&
                 HasAnnotation(entry.Metadata, PersistenceAnnotations.AuditStamps)) ||
                (HasAnnotation(entry.Metadata, PersistenceAnnotations.SoftDelete) &&
                 NeedsSoftDeleteStamp(entry)))
            .ToArray();

        if (stampedEntries.Length == 0)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var actor = auditActorResolver.Resolve(userId);

        foreach (var entry in stampedEntries)
        {
            var hasAuditStamps = HasAnnotation(entry.Metadata, PersistenceAnnotations.AuditStamps);

            if (entry.State == EntityState.Added && hasAuditStamps)
            {
                entry.Property(AuditStampNames.CreatedAt).CurrentValue = now;
                entry.Property(AuditStampNames.CreatedBy).CurrentValue = actor;
            }

            if (entry.State == EntityState.Modified && hasAuditStamps)
            {
                entry.Property(AuditStampNames.UpdatedAt).CurrentValue = now;
                entry.Property(AuditStampNames.UpdatedBy).CurrentValue = actor;
            }

            if (HasAnnotation(entry.Metadata, PersistenceAnnotations.SoftDelete) &&
                NeedsSoftDeleteStamp(entry))
            {
                var isDeleted = entry.Property(SoftDeleteNames.IsDeleted).CurrentValue is true;

                entry.Property(SoftDeleteNames.DeletedAt).CurrentValue = isDeleted ? now : null;
                entry.Property(SoftDeleteNames.DeletedBy).CurrentValue = isDeleted ? actor : null;
            }
        }
    }

    // A modified row is stamped when its flag changed; a row added already
    // deleted (an import) is stamped when the import left the stamp empty.
    private static bool NeedsSoftDeleteStamp(EntityEntry entry)
    {
        var flag = entry.Property(SoftDeleteNames.IsDeleted);

        return entry.State switch
        {
            EntityState.Modified => !Equals(flag.OriginalValue, flag.CurrentValue),
            EntityState.Added => flag.CurrentValue is true &&
                                 entry.Property(SoftDeleteNames.DeletedAt).CurrentValue is null,
            _ => false
        };
    }
}
