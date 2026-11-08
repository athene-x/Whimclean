namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence;

/// <summary>
/// Model annotations that opt an entity type into the behaviors
/// <see cref="AuditableDbContext{TUserId, TActor}" /> applies. Set them with
/// the <see cref="EntityTypeBuilderExtensions" /> rather than by name.
/// </summary>
public static class PersistenceAnnotations
{
    /// <summary>The entity type carries the audit stamps.</summary>
    public const string AuditStamps = "Whimclean:AuditStamps";

    /// <summary>The entity type is soft-deleted instead of removed.</summary>
    public const string SoftDelete = "Whimclean:SoftDelete";
}
