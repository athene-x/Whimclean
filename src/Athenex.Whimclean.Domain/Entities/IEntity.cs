namespace Athenex.Whimclean.Domain.Entities;

/// <summary>
/// Identifies a domain object that is distinguished by identity rather
/// than by attribute values.
/// </summary>
/// <typeparam name="TId">
/// The identifier type; its default value is reserved to mean "no
/// identity assigned yet".
/// </typeparam>
public interface IEntity<out TId>
    where TId : notnull
{
    /// <summary>
    /// The identity this entity is compared by. The default value of
    /// <typeparamref name="TId" /> marks a transient (not yet persisted)
    /// instance.
    /// </summary>
    TId Id { get; }
}
