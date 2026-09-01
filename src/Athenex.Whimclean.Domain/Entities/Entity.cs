using System.Runtime.CompilerServices;

namespace Athenex.Whimclean.Domain.Entities;

/// <summary>
/// Base class for domain objects whose identity, not attribute values,
/// defines equality. Two entities are equal exactly when they are of the
/// same runtime type and carry the same non-default <see cref="Id" />;
/// an entity whose <see cref="Id" /> is still the default value is
/// transient and equals nothing but itself (by reference).
/// </summary>
/// <typeparam name="TId">
/// The identifier type; its default value is reserved to mean "no
/// identity assigned yet".
/// </typeparam>
public abstract class Entity<TId> : IEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// The identity this entity is compared by. The default value of
    /// <typeparamref name="TId" /> marks the entity as transient (not yet
    /// persisted).
    /// </summary>
    public TId Id { get; private set; } = default!;

    /// <summary>
    /// Initializes the entity with an established identity.
    /// </summary>
    /// <param name="id">The identifier; must not be the default value.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="id" /> is the default value of
    /// <typeparamref name="TId" />.
    /// </exception>
    protected Entity(TId id)
    {
        if (EqualityComparer<TId>.Default.Equals(id, default!))
        {
            throw new ArgumentException(
                "Entity ID cannot be the default value.",
                nameof(id));
        }

        Id = id;
    }

    /// <summary>
    /// Initializes a transient entity. Exists for ORM materialization;
    /// domain code should use the identity-taking constructor.
    /// </summary>
    protected Entity()
    {
    }

    /// <summary>
    /// Determines identity-based equality: <see langword="true" /> only
    /// when <paramref name="obj" /> is an entity of the same runtime type
    /// with the same non-default <see cref="Id" />, or the same instance.
    /// Transient entities (default <see cref="Id" />) never equal a
    /// distinct instance.
    /// </summary>
    /// <param name="obj">The object to compare against; may be null.</param>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        if (HasDefaultId || other.HasDefaultId)
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <summary>
    /// Returns a hash consistent with <see cref="Equals(object?)" />:
    /// derived from the runtime type and <see cref="Id" /> once an
    /// identity is assigned, and from the reference while transient — so
    /// the hash changes when a transient entity later gains an identity.
    /// </summary>
    public override int GetHashCode()
    {
        return HasDefaultId
            ? RuntimeHelpers.GetHashCode(this)
            : HashCode.Combine(GetType(), Id);
    }

    /// <summary>
    /// Identity-based equality with null tolerance: two nulls are equal,
    /// a null and an instance are not, and two instances compare via
    /// <see cref="Equals(object?)" />.
    /// </summary>
    /// <param name="left">The first entity; may be null.</param>
    /// <param name="right">The second entity; may be null.</param>
    public static bool operator ==(
        Entity<TId>? left,
        Entity<TId>? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// The negation of <see cref="operator ==" />.
    /// </summary>
    /// <param name="left">The first entity; may be null.</param>
    /// <param name="right">The second entity; may be null.</param>
    public static bool operator !=(
        Entity<TId>? left,
        Entity<TId>? right)
    {
        return !Equals(left, right);
    }

    private bool HasDefaultId =>
        EqualityComparer<TId>.Default.Equals(Id, default!);
}
