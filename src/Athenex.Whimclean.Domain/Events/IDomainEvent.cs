namespace Athenex.Whimclean.Domain.Events;

/// <summary>
/// Marks a fact that happened inside the domain and is of interest beyond
/// the aggregate that raised it. Events are queued on the aggregate and
/// dispatched by the infrastructure after the unit of work commits.
/// </summary>
public interface IDomainEvent
{
}
