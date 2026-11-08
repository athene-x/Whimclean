namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence;

/// <summary>
/// Names of the soft-delete properties that
/// <see cref="AuditableDbContext{TUserId, TActor}" /> maintains on entity
/// types configured with
/// <see cref="EntityTypeBuilderExtensions.HasSoftDelete(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder)" />.
/// They are shadow properties unless the entity declares a CLR property of
/// the same name and type. Read one in a query with
/// <c>EF.Property&lt;bool&gt;(entity, SoftDeleteNames.IsDeleted)</c>.
/// </summary>
public static class SoftDeleteNames
{
    /// <summary>Whether the row is hidden by the soft-delete filter; <c>bool</c>.</summary>
    public const string IsDeleted = "IsDeleted";

    /// <summary>UTC instant of the soft delete; nullable <c>DateTimeOffset</c>.</summary>
    public const string DeletedAt = "DeletedAt";

    /// <summary>Actor that soft-deleted the row; nullable <c>TActor</c>.</summary>
    public const string DeletedBy = "DeletedBy";
}
