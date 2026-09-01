namespace Athenex.Whimclean.Domain.Behaviors;

/// <summary>
/// Identifies a domain object that can be toggled between active and
/// inactive by an operator.
///
/// Unlike <see cref="ISoftDelete{TActor}" /> — which hides a record from
/// every default query while existing references keep resolving — an
/// inactive record stays fully visible to operational callers (e.g. an
/// admin panel, where it can be toggled back) and is excluded only from
/// front-usage queries such as selection lists. Because visibility depends
/// on the caller, no global query filter is applied; front-usage queries
/// opt in explicitly via <c>WhereActive()</c>.
/// </summary>
public interface IActivatable
{
    /// <summary>
    /// Whether the record is currently offered to front-usage callers.
    /// When <see langword="false" /> the record remains visible and
    /// editable to operational callers but is excluded from queries that
    /// opt in via <c>WhereActive()</c>.
    /// </summary>
    bool IsActive { get; }
}
