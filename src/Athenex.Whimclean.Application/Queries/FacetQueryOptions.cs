namespace Athenex.Whimclean.Application.Queries;

/// <summary>
/// Validated, normalized input for computing the distinct values (a facet)
/// of one field under the same search and filter constraints as a list
/// query. Limits mirror <see cref="ListQueryOptions" />; the number of
/// values returned is bounded by <see cref="Limit" />.
/// </summary>
public sealed class FacetQueryOptions
{
    /// <summary>
    /// Number of facet values returned when the caller specifies no limit.
    /// </summary>
    public const int DefaultLimit = 100;

    /// <summary>Largest accepted facet value limit.</summary>
    public const int MaximumLimit = 1000;

    /// <summary>
    /// Validates and normalizes the raw input: field and search term are
    /// trimmed, a blank search term becomes <see langword="null" />, and
    /// filters are materialized.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="field" /> is <see langword="null" /> or whitespace,
    /// the trimmed search term exceeds
    /// <see cref="ListQueryOptions.MaximumSearchLength" /> characters, more
    /// than <see cref="ListQueryOptions.MaximumFilters" /> filters were
    /// supplied, or <paramref name="filters" /> contains a
    /// <see langword="null" /> element.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="limit" /> is less than 1 or exceeds
    /// <see cref="MaximumLimit" />.
    /// </exception>
    public FacetQueryOptions(
        string field,
        string? searchTerm = null,
        SortDirection direction = SortDirection.Ascending,
        IEnumerable<FilterTerm>? filters = null,
        int limit = DefaultLimit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, MaximumLimit);

        Field = field.Trim();
        SearchTerm = QueryValidation.NormalizeSearchTerm(
            searchTerm,
            nameof(searchTerm));
        Filters = Array.AsReadOnly(QueryValidation.MaterializeTerms(
            filters,
            ListQueryOptions.MaximumFilters,
            "filters",
            nameof(filters)));
        Direction = direction;
        Limit = limit;
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

    /// <summary>
    /// Largest number of distinct values the query may return; between 1
    /// and <see cref="MaximumLimit" />, <see cref="DefaultLimit" /> unless
    /// specified otherwise.
    /// </summary>
    public int Limit { get; }
}
