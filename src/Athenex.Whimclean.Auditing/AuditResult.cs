namespace Athenex.Whimclean.Auditing;

/// <summary>Outcome of an audited command.</summary>
public enum AuditResult
{
    /// <summary>The command completed and its changes were committed.</summary>
    Success = 0,

    /// <summary>
    /// The command returned a failed result or threw; its changes were
    /// not committed.
    /// </summary>
    Failed = 1
}
