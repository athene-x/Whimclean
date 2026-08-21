using System.Linq.Expressions;
using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Infrastructure.Auditing;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence;
using Athenex.Whimclean.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy.Persistence;

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
    public bool HasCurrentTenant =>
        currentTenant.TryGetTenantId(out _);

    public TTenantId CurrentTenantId =>
        currentTenant.TryGetTenantId(out var tenantId)
            ? tenantId
            : default!;

    protected sealed override void ConfigureAdditionalInfrastructureModel(
        ModelBuilder modelBuilder)
    {
        ApplyTenantQueryFilters(modelBuilder);
    }

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
                "TenantFilter",
                Expression.Lambda(filterBody, parameter));
        }
    }
}
