namespace Athenex.Whimclean.Domain.Entities;

public interface IEntity<out TId>
    where TId : notnull
{
    TId Id { get; }
}
