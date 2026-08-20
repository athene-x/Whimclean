namespace Athenex.Whimclean.Application.Queries;

public sealed class ListQueryOptions
{
    public const int DefaultPage = 1;
    public const int DefaultSize = 10;
    public const int MaximumSize = 100;
    public const int MaximumSearchLength = 200;
    public const int MaximumSortTerms = 5;
    public const int MaximumFilters = 10;

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

        var normalizedSearchTerm = string.IsNullOrWhiteSpace(searchTerm)
            ? null
            : searchTerm.Trim();

        if (normalizedSearchTerm?.Length > MaximumSearchLength)
        {
            throw new ArgumentException(
                $"The search term cannot exceed {MaximumSearchLength} characters.",
                nameof(searchTerm));
        }

        var materializedSort = sort?.ToArray() ?? [];
        var materializedFilters = filters?.ToArray() ?? [];

        if (materializedSort.Length > MaximumSortTerms)
        {
            throw new ArgumentException(
                $"A query cannot contain more than {MaximumSortTerms} sort terms.",
                nameof(sort));
        }

        if (materializedFilters.Length > MaximumFilters)
        {
            throw new ArgumentException(
                $"A query cannot contain more than {MaximumFilters} filters.",
                nameof(filters));
        }

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

    public string? SearchTerm { get; }
    public IReadOnlyList<SortTerm> Sort { get; }
    public IReadOnlyList<FilterTerm> Filters { get; }
    public int Page { get; }
    public int Size { get; }
    public int Offset { get; }
}
