namespace Athenex.Whimclean.Application.Queries;

/// <summary>
/// One normalized field comparison of a list or facet query. The field is
/// stored trimmed; values are stored verbatim so comparisons against data
/// with significant whitespace remain possible. Single-value operators
/// compare against <see cref="Value" />; <see cref="FilterOperator.In" />
/// matches any of <see cref="Values" />.
/// </summary>
public sealed record FilterTerm
{
    /// <summary>
    /// Largest accepted number of values for an
    /// <see cref="FilterOperator.In" /> term.
    /// </summary>
    public const int MaximumValues = 25;

    /// <summary>
    /// Creates a single-value term, trimming <paramref name="field" />;
    /// the operator defaults to <see cref="FilterOperator.Equal" />.
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
        Values = Array.AsReadOnly([value]);
        Operator = @operator;
    }

    /// <summary>
    /// Creates an <see cref="FilterOperator.In" /> term matching any of
    /// <paramref name="values" />, trimming <paramref name="field" />.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="field" /> is <see langword="null" /> or whitespace,
    /// <paramref name="values" /> is empty, holds more than
    /// <see cref="MaximumValues" /> elements, or contains a
    /// <see langword="null" /> element.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="values" /> is <see langword="null" />.
    /// </exception>
    public FilterTerm(
        string field,
        IEnumerable<string> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentNullException.ThrowIfNull(values);

        var materializedValues = values.ToArray();

        if (materializedValues.Length == 0)
        {
            throw new ArgumentException(
                "An in-filter must contain at least one value.",
                nameof(values));
        }

        if (materializedValues.Length > MaximumValues)
        {
            throw new ArgumentException(
                $"An in-filter cannot contain more than {MaximumValues} values.",
                nameof(values));
        }

        if (Array.Exists(materializedValues, value => value is null))
        {
            throw new ArgumentException(
                "The values cannot contain a null element.",
                nameof(values));
        }

        Field = field.Trim();
        Values = Array.AsReadOnly(materializedValues);
        Operator = FilterOperator.In;
    }

    /// <summary>Trimmed name of the field being compared.</summary>
    public string Field { get; }

    /// <summary>
    /// Values the field is compared against, stored verbatim; always at
    /// least one, at most <see cref="MaximumValues" />. Single-value
    /// operators use only the first.
    /// </summary>
    public IReadOnlyList<string> Values { get; }

    /// <summary>
    /// The single comparison value, stored verbatim; may be empty, never
    /// <see langword="null" />. Equivalent to the first element of
    /// <see cref="Values" />.
    /// </summary>
    public string Value => Values[0];

    /// <summary>
    /// Comparison to apply; <see cref="FilterOperator.Equal" /> unless
    /// specified otherwise, always <see cref="FilterOperator.In" /> for
    /// multi-value terms.
    /// </summary>
    public FilterOperator Operator { get; }

    /// <summary>
    /// Compares by field, operator, and value sequence, replacing the
    /// generated record equality that would compare the values list by
    /// reference.
    /// </summary>
    public bool Equals(FilterTerm? other) =>
        other is not null &&
        Field == other.Field &&
        Operator == other.Operator &&
        Values.SequenceEqual(other.Values, StringComparer.Ordinal);

    /// <summary>
    /// Hashes the field, operator, and value sequence, consistent with
    /// <see cref="Equals(FilterTerm)" />.
    /// </summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Field, StringComparer.Ordinal);
        hash.Add(Operator);

        foreach (var value in Values)
        {
            hash.Add(value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
