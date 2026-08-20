using System.Runtime.CompilerServices;

namespace Athenex.Whimclean.Domain.Entities;

public abstract class Entity<TId> : IEntity<TId>
    where TId : notnull
{
    public TId Id { get; private set; } = default!;

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

    protected Entity()
    {
    }

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

    public override int GetHashCode()
    {
        return HasDefaultId
            ? RuntimeHelpers.GetHashCode(this)
            : HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(
        Entity<TId>? left,
        Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(
        Entity<TId>? left,
        Entity<TId>? right)
    {
        return !Equals(left, right);
    }

    private bool HasDefaultId =>
        EqualityComparer<TId>.Default.Equals(Id, default!);
}
