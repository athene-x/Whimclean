namespace Athenex.Whimclean.Domain.Results;

/// <summary>
/// The semantic category of an <see cref="Error" />, used by outer layers
/// to decide how a failure is surfaced to callers.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Not an error. Reserved for the <see cref="Error.None" /> sentinel
    /// carried by successful results; regular errors never use it.
    /// </summary>
    None = 0,

    /// <summary>
    /// An unexpected or unclassified fault — the category of last resort
    /// when nothing more specific applies.
    /// </summary>
    Failure = 1,

    /// <summary>
    /// The input was malformed or violated a business rule, so the
    /// operation was never attempted.
    /// </summary>
    Validation = 2,

    /// <summary>
    /// The requested resource does not exist or is not visible to the
    /// caller.
    /// </summary>
    NotFound = 3,

    /// <summary>
    /// The operation clashes with the current state of the system (e.g.
    /// a duplicate, or a concurrent change).
    /// </summary>
    Conflict = 4,

    /// <summary>
    /// The caller lacks a valid identity — who they are could not be
    /// established.
    /// </summary>
    Unauthorized = 5,

    /// <summary>
    /// The caller's identity is known but they are not permitted to
    /// perform the operation.
    /// </summary>
    Forbidden = 6
}
