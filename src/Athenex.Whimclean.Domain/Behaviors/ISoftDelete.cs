namespace Athenex.Whimclean.Domain.Behaviors;

/// <summary>
/// Identifies a record that is retired by flagging rather than by physical
/// removal, so existing references keep resolving. Soft-deleted records
/// are hidden from every default query by a global query filter in the EF
/// infrastructure package (visibility fails closed); contrast with
/// <see cref="IActivatable" />, which leaves visibility to the caller.
/// The contract is read-only: the infrastructure stamps the values through
/// the change tracker at save time.
/// </summary>
/// <typeparam name="TActor">
/// The stored actor type (e.g. a user id). May itself be a nullable type
/// when system-initiated deletes leave the actor column unset.
/// </typeparam>
public interface ISoftDelete<TActor>
{
    /// <summary>
    /// Whether the record has been soft-deleted. When
    /// <see langword="true" /> the record is excluded from default queries
    /// by the global query filter.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// The moment the record was soft-deleted, or <see langword="null" />
    /// while the record is not deleted.
    /// </summary>
    DateTimeOffset? DeletedAt { get; }

    /// <summary>
    /// The actor on whose behalf the record was soft-deleted, or
    /// <see langword="null" /> while the record is not deleted.
    /// </summary>
    TActor? DeletedBy { get; }
}
