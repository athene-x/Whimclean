namespace Athenex.Whimclean.Application.Queries;

/// <summary>
/// One normalized field/value comparison of a list or facet query; the
/// field and value are stored trimmed.
/// </summary>
public sealed record FilterTerm
{
    /// <summary>
    /// Creates the term, trimming <paramref name="field" /> and
    /// <paramref name="value" />; the operator defaults to
    /// <see cref="FilterOperator.Equal" />.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="field" /> is <see langword="null" /> or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public FilterTerm(
        string field,
        string value,
        FilterOperator @operator = FilterOperator.Equal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentNullException.ThrowIfNull(value);

        Field = field.Trim();
        Value = value.Trim();
        Operator = @operator;
    }

    /// <summary>Trimmed name of the field being compared.</summary>
    public string Field { get; }

    /// <summary>
    /// Trimmed value the field is compared against; may be empty, never
    /// <see langword="null" />.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Comparison to apply; <see cref="FilterOperator.Equal" /> unless
    /// specified otherwise.
    /// </summary>
    public FilterOperator Operator { get; }
}
