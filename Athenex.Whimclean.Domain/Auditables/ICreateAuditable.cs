namespace Athenex.Whimclean.Domain.Auditables;

public interface ICreateAuditable<TActor>
{
    DateTimeOffset CreatedAt { get; }
    TActor CreatedBy { get; }
}
