namespace Athenex.Whimclean.Application.Queries;

/// <summary>
/// Validated, normalized input for computing the distinct values (a facet)
/// of one field under the same search and filter constraints as a list
/// query. Limits mirror <see cref="ListQueryOptions" />.
/// </summary>
public sealed class FacetQueryOptions
{
    /// <summary>
    /// Validates and normalizes the raw input: field and search term are
    /// trimmed, a blank search term becomes <see langword="null" />, and
    /// filters are materialized.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="field" /> is <see langword="null" /> or whitespace,
    /// the trimmed search term exceeds
    /// <see cref="ListQueryOptions.MaximumSearchLength" /> characters, or
    /// more than <see cref="ListQueryOptions.MaximumFilters" /> filters
    /// were supplied.
    /// </exception>
    public FacetQueryOptions(
        string field,
        string? searchTerm = null,
        SortDirection direction = SortDirection.Ascending,
        IEnumerable<FilterTerm>? filters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        var normalizedSearchTerm = string.IsNullOrWhiteSpace(searchTerm)
            ? null
            : searchTerm.Trim();

        if (normalizedSearchTerm?.Length > ListQueryOptions.MaximumSearchLength)
        {
            throw new ArgumentException(
                $"The search term cannot exceed " +
                $"{ListQueryOptions.MaximumSearchLength} characters.",
                nameof(searchTerm));
        }

        var materializedFilters = filters?.ToArray() ?? [];
        if (materializedFilters.Length > ListQueryOptions.MaximumFilters)
        {
            throw new ArgumentException(
                $"A query cannot contain more than " +
                $"{ListQueryOptions.MaximumFilters} filters.",
                nameof(filters));
        }

        Field = field.Trim();
        SearchTerm = normalizedSearchTerm;
        Filters = Array.AsReadOnly(materializedFilters);
        Direction = direction;
    }

    /// <summary>
    /// Trimmed name of the field whose distinct values are requested.
    /// </summary>
    public string Field { get; }

    /// <summary>
    /// Trimmed search text constraining the records the facet is computed
    /// over, or <see langword="null" /> when none was supplied.
    /// </summary>
    public string? SearchTerm { get; }

    /// <summary>
    /// Read-only filters constraining the records the facet is computed
    /// over; at most <see cref="ListQueryOptions.MaximumFilters" />, empty
    /// when none were supplied.
    /// </summary>
    public IReadOnlyList<FilterTerm> Filters { get; }

    /// <summary>
    /// Order the facet values are returned in; ascending by default.
    /// </summary>
    public SortDirection Direction { get; }
}
