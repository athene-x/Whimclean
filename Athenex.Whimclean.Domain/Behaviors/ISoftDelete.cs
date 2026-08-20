namespace Athenex.Whimclean.Domain.Behaviors;

public interface ISoftDelete<TActor>
{
    bool IsDeleted { get; }

    DateTimeOffset? DeletedAt { get; }
    TActor DeletedBy { get; }
}
