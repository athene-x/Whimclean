namespace Athenex.Whimclean.Domain.Auditables;

/// <summary>
/// Identifies a record whose modifications must be attributable. The
/// contract is read-only: the EF infrastructure package stamps both values
/// through the change tracker each time the record is updated.
/// </summary>
/// <typeparam name="TActor">
/// The stored actor type (e.g. a user id). May itself be a nullable type
/// when system-initiated writes leave the actor column unset.
/// </typeparam>
public interface IUpdateAuditable<TActor>
{
    /// <summary>
    /// The moment of the most recent update, or <see langword="null" />
    /// while the record has never been modified since creation.
    /// </summary>
    DateTimeOffset? UpdatedAt { get; }

    /// <summary>
    /// The actor on whose behalf the most recent update was made, or
    /// <see langword="null" /> while the record has never been modified
    /// since creation.
    /// </summary>
    TActor? UpdatedBy { get; }
}
