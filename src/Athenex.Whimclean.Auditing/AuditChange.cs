namespace Athenex.Whimclean.Auditing;

/// <summary>One field change recorded on a successful command.</summary>
/// <param name="Field">Name of the field.</param>
/// <param name="OldValue">Value before the command.</param>
/// <param name="NewValue">Value after the command.</param>
public sealed record AuditChange(
    string Field,
    object? OldValue,
    object? NewValue);
