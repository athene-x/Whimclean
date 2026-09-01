namespace Athenex.Whimclean.Application.Queries;

/// <summary>
/// One normalized ordering instruction of a list query; the field is
/// stored trimmed.
/// </summary>
public sealed record SortTerm
{
    /// <summary>
    /// Creates the term, trimming <paramref name="field" />; the direction
    /// defaults to <see cref="SortDirection.Ascending" />.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="field" /> is <see langword="null" /> or whitespace.
    /// </exception>
    public SortTerm(
        string field,
        SortDirection direction = SortDirection.Ascending)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        Field = field.Trim();
        Direction = direction;
    }

    /// <summary>Trimmed name of the field being ordered by.</summary>
    public string Field { get; }

    /// <summary>
    /// Order to apply; <see cref="SortDirection.Ascending" /> unless
    /// specified otherwise.
    /// </summary>
    public SortDirection Direction { get; }
}
