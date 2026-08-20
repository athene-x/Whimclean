namespace Athenex.Whimclean.Domain.Auditables;

public interface IUpdateAuditable<TActor>
{
    DateTimeOffset? UpdatedAt { get; }
    TActor UpdatedBy { get; }
}
