namespace Athenex.Whimclean.Auditing;

/// <summary>
/// A command whose outcome is written to the audit log, successful or
/// not. Implement it on the Mediator command; the
/// <see cref="AuditCommandBehavior{TMessage, TResponse}" /> does the rest.
/// </summary>
public interface IAuditedCommand
{
    /// <summary>Describes the command for its audit entry.</summary>
    AuditDescriptor Describe();
}
