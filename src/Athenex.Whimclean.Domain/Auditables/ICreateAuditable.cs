namespace Athenex.Whimclean.Domain.Auditables;

/// <summary>
/// Identifies a record whose creation must be attributable. The contract
/// is read-only: the EF infrastructure package stamps both values through
/// the change tracker when the record is first saved.
/// </summary>
/// <typeparam name="TActor">
/// The stored actor type (e.g. a user id). May itself be a nullable type
/// when system-initiated writes leave the actor column unset.
/// </typeparam>
public interface ICreateAuditable<TActor>
{
    /// <summary>
    /// The moment the record was first persisted. Stamped once by the
    /// infrastructure and never changed afterwards.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// The actor on whose behalf the record was created. Stamped once by
    /// the infrastructure alongside <see cref="CreatedAt" />.
    /// </summary>
    TActor CreatedBy { get; }
}
