namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence;

/// <summary>
/// Names of the audit-stamp properties that
/// <see cref="AuditableDbContext{TUserId, TActor}" /> maintains on entity
/// types configured with
/// <see cref="EntityTypeBuilderExtensions.HasAuditStamps(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder)" />.
/// The properties are shadow properties unless the entity declares a CLR
/// property of the same name and type, in which case that property is
/// stamped instead. Read a shadow value in a query with
/// <c>EF.Property&lt;DateTimeOffset&gt;(entity, AuditStampNames.CreatedAt)</c>.
/// </summary>
public static class AuditStampNames
{
    /// <summary>UTC instant the row was inserted; <c>DateTimeOffset</c>.</summary>
    public const string CreatedAt = "CreatedAt";

    /// <summary>Actor that inserted the row; the context's <c>TActor</c>.</summary>
    public const string CreatedBy = "CreatedBy";

    /// <summary>UTC instant of the last update; nullable <c>DateTimeOffset</c>.</summary>
    public const string UpdatedAt = "UpdatedAt";

    /// <summary>Actor of the last update; nullable <c>TActor</c>.</summary>
    public const string UpdatedBy = "UpdatedBy";
}
