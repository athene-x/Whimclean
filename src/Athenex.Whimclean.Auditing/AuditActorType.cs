namespace Athenex.Whimclean.Auditing;

/// <summary>Who performed an audited action.</summary>
public enum AuditActorType
{
    /// <summary>A signed-in person.</summary>
    User = 0,

    /// <summary>The application itself: startup bootstrap, a scheduled job.</summary>
    System = 1,

    /// <summary>Another service calling with its own credentials.</summary>
    Service = 2
}
