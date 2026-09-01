using System.Linq.Expressions;
using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Infrastructure.Auditing;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence;
using Athenex.Whimclean.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy.Persistence;

/// <summary>
/// <see cref="AuditableDbContext{TUserId, TActor}" /> that adds tenant
/// isolation. Reads: the named tenant query filter
/// (<see cref="WhimcleanQueryFilters.Tenant" />) makes
/// <see cref="IMultiTenant{TTenantId}" /> rows visible only when a current
/// tenant is established AND matches the row — with no tenant the filter
/// fails closed and returns nothing. Writes: any change to an
/// <see cref="IMultiTenant{TTenantId}" /> entity requires a current tenant
/// and a matching <c>TenantId</c>.
/// </summary>
/// <typeparam name="TUserId">
/// Identifier type reported by <see cref="ICurrentUser{TUserId}" />.
/// </typeparam>
/// <typeparam name="TActor">
/// Value stored in the audit actor columns.
/// </typeparam>
/// <typeparam name="TTenantId">Tenant identifier type.</typeparam>
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
/// <param name="currentTenant">
/// Source of the tenant reads and writes are scoped to.
/// </param>
public abstract class MultiTenantDbContext<TUserId, TActor, TTenantId>(
    DbContextOptions options,
    TimeProvider timeProvider,
    ICurrentUser<TUserId> currentUser,
    IAuditActorResolver<TUserId, TActor> auditActorResolver,
    IDomainEventDispatcher domainEventDispatcher,
    ICurrentTenant<TTenantId> currentTenant)
    : AuditableDbContext<TUserId, TActor>(
        options,
        timeProvider,
        currentUser,
        auditActorResolver,
        domainEventDispatcher)
    where TUserId : notnull
    where TTenantId : notnull
{
    /// <summary>
    /// Whether a current tenant is established. Public only so the
    /// compiled tenant query filter can reference it — not intended as a
    /// consumer API.
    /// </summary>
    public bool HasCurrentTenant =>
        currentTenant.TryGetTenantId(out _);

    /// <summary>
    /// The established tenant's identifier, or <c>default</c> when none
    /// is established. Public only so the compiled tenant query filter can
    /// reference it — not intended as a consumer API; the filter guards it
    /// with <see cref="HasCurrentTenant" />.
    /// </summary>
    public TTenantId CurrentTenantId =>
        currentTenant.TryGetTenantId(out var tenantId)
            ? tenantId
            : default!;

    /// <summary>
    /// Sealed: installs the named tenant query filter
    /// (<see cref="WhimcleanQueryFilters.Tenant" />) on root
    /// <see cref="IMultiTenant{TTenantId}" /> entity types.
    /// </summary>
    protected sealed override void ConfigureAdditionalInfrastructureModel(
        ModelBuilder modelBuilder)
    {
        ApplyTenantQueryFilters(modelBuilder);
    }

    /// <summary>
    /// Sealed: rejects any pending change to an
    /// <see cref="IMultiTenant{TTenantId}" /> entity when no current
    /// tenant is established or when the entity belongs to a different
    /// tenant. A no-op when no tenant-owned entities changed.
    /// </summary>
    /// <exception cref="CurrentTenantRequiredException">
    /// Tenant-owned changes are pending but no current tenant is
    /// established.
    /// </exception>
    /// <exception cref="TenantAccessViolationException">
    /// A changed entity's <c>TenantId</c> differs from the current
    /// tenant's.
    /// </exception>
    protected sealed override void ValidateInfrastructureWrites()
    {
        var tenantEntries = ChangeTracker
            .Entries<IMultiTenant<TTenantId>>()
            .Where(entry =>
                entry.State is EntityState.Added or
                    EntityState.Modified or
                    EntityState.Deleted)
            .ToArray();

        if (tenantEntries.Length == 0)
        {
            return;
        }

        if (!currentTenant.TryGetTenantId(out var tenantId))
        {
            throw new CurrentTenantRequiredException();
        }

        var invalidEntry = tenantEntries.FirstOrDefault(entry =>
            !EqualityComparer<TTenantId>.Default.Equals(
                entry.Entity.TenantId,
                tenantId));

        if (invalidEntry is not null)
        {
            throw new TenantAccessViolationException();
        }
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        var multiTenantType = typeof(IMultiTenant<TTenantId>);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!multiTenantType.IsAssignableFrom(entityType.ClrType) ||
                (entityType.BaseType is not null &&
                 multiTenantType.IsAssignableFrom(entityType.BaseType.ClrType)))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var tenantId = Expression.Property(
                parameter,
                nameof(IMultiTenant<TTenantId>.TenantId));
            var hasCurrentTenant = Expression.Property(
                Expression.Constant(this),
                nameof(HasCurrentTenant));
            var currentTenantId = Expression.Property(
                Expression.Constant(this),
                nameof(CurrentTenantId));
            var belongsToCurrentTenant = Expression.Equal(
                tenantId,
                currentTenantId);
            var filterBody = Expression.AndAlso(
                hasCurrentTenant,
                belongsToCurrentTenant);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                WhimcleanQueryFilters.Tenant,
                Expression.Lambda(filterBody, parameter));
        }
    }
}
