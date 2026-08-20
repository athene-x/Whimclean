namespace Athenex.Whimclean.Domain.Auditables;

public interface IAuditable<TActor> : ICreateAuditable<TActor>, IUpdateAuditable<TActor>
{
}
