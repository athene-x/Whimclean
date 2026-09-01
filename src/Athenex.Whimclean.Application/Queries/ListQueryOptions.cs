namespace Athenex.Whimclean.Application.Queries;

/// <summary>
/// Validated, normalized input for an offset-paginated list query: a
/// trimmed search term, capped sort and filter terms, and a precomputed
/// <see cref="Offset" />. Construct it at the boundary so query handlers
/// can trust every value without re-validating.
/// </summary>
public sealed class ListQueryOptions
{
    /// <summary>Page requested when the caller specifies none.</summary>
    public const int DefaultPage = 1;

    /// <summary>Page size used when the caller specifies none.</summary>
    public const int DefaultSize = 10;

    /// <summary>Largest accepted page size.</summary>
    public const int MaximumSize = 100;

    /// <summary>
    /// Longest accepted search term, in characters after trimming.
    /// </summary>
    public const int MaximumSearchLength = 200;

    /// <summary>Largest accepted number of sort terms per query.</summary>
    public const int MaximumSortTerms = 5;

    /// <summary>Largest accepted number of filters per query.</summary>
    public const int MaximumFilters = 10;

    /// <summary>
    /// Validates and normalizes the raw input: the search term is trimmed
    /// (blank becomes <see langword="null" />), sort and filter sequences
    /// are materialized, and <see cref="Offset" /> is precomputed.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="page" /> or <paramref name="size" /> is less than 1,
    /// <paramref name="size" /> exceeds <see cref="MaximumSize" />, or the
    /// page's offset would overflow the supported pagination range.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The trimmed search term exceeds
    /// <see cref="MaximumSearchLength" /> characters, more than
    /// <see cref="MaximumSortTerms" /> sort terms were supplied, more than
    /// <see cref="MaximumFilters" /> filters were supplied, or a sort or
    /// filter sequence contains a <see langword="null" /> element.
    /// </exception>
    public ListQueryOptions(
        string? searchTerm = null,
        IEnumerable<SortTerm>? sort = null,
        IEnumerable<FilterTerm>? filters = null,
        int page = DefaultPage,
        int size = DefaultSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, MaximumSize);

        var normalizedSearchTerm = QueryValidation.NormalizeSearchTerm(
            searchTerm,
            nameof(searchTerm));

        var materializedSort = QueryValidation.MaterializeTerms(
            sort,
            MaximumSortTerms,
            "sort terms",
            nameof(sort));

        var materializedFilters = QueryValidation.MaterializeTerms(
            filters,
            MaximumFilters,
            "filters",
            nameof(filters));

        var offset = ((long)page - 1) * size;
        if (offset > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(page),
                "The requested page exceeds the supported pagination range.");
        }

        SearchTerm = normalizedSearchTerm;
        Sort = Array.AsReadOnly(materializedSort);
        Filters = Array.AsReadOnly(materializedFilters);
        Page = page;
        Size = size;
        Offset = (int)offset;
    }

    /// <summary>
    /// Trimmed free-text search term, or <see langword="null" /> when the
    /// caller supplied none (or only whitespace).
    /// </summary>
    public string? SearchTerm { get; }

    /// <summary>
    /// Read-only ordering terms, applied in sequence; at most
    /// <see cref="MaximumSortTerms" />, empty when none were supplied.
    /// </summary>
    public IReadOnlyList<SortTerm> Sort { get; }

    /// <summary>
    /// Read-only filters, all of which a record must satisfy; at most
    /// <see cref="MaximumFilters" />, empty when none were supplied.
    /// </summary>
    public IReadOnlyList<FilterTerm> Filters { get; }

    /// <summary>One-based requested page; always at least 1.</summary>
    public int Page { get; }

    /// <summary>
    /// Requested page size; between 1 and <see cref="MaximumSize" />.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Precomputed number of records to skip:
    /// (<see cref="Page" /> - 1) * <see cref="Size" />.
    /// </summary>
    public int Offset { get; }
}
