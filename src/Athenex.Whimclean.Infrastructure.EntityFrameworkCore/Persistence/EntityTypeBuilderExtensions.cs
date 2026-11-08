using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence;

/// <summary>
/// Opts entity types into the audit stamping and soft deletion that
/// <see cref="AuditableDbContext{TUserId, TActor}" /> performs. Call these
/// from <c>ConfigureModel</c>; the context adds the properties (see
/// <see cref="AuditStampNames" /> and <see cref="SoftDeleteNames" />) with
/// its own actor type after the application model is configured, so the
/// domain model never declares them.
/// </summary>
public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// Marks the entity type for audit stamping: the context sets
    /// <c>CreatedAt</c>/<c>CreatedBy</c> on insert and
    /// <c>UpdatedAt</c>/<c>UpdatedBy</c> on update.
    /// </summary>
    public static EntityTypeBuilder HasAuditStamps(this EntityTypeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasAnnotation(PersistenceAnnotations.AuditStamps, true);

        return builder;
    }

    /// <inheritdoc cref="HasAuditStamps(EntityTypeBuilder)" />
    public static EntityTypeBuilder<TEntity> HasAuditStamps<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        ((EntityTypeBuilder)builder).HasAuditStamps();

        return builder;
    }

    /// <summary>
    /// Marks the entity type for soft deletion: <c>Remove</c> sets
    /// <c>IsDeleted</c>, <c>DeletedAt</c> and <c>DeletedBy</c> instead of
    /// deleting the row, and the named query filter
    /// <see cref="QueryFilterNames.SoftDelete" /> hides deleted rows.
    /// </summary>
    public static EntityTypeBuilder HasSoftDelete(this EntityTypeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasAnnotation(PersistenceAnnotations.SoftDelete, true);

        return builder;
    }

    /// <inheritdoc cref="HasSoftDelete(EntityTypeBuilder)" />
    public static EntityTypeBuilder<TEntity> HasSoftDelete<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        ((EntityTypeBuilder)builder).HasSoftDelete();

        return builder;
    }
}
