namespace Athenex.Whimclean.Application.Queries;

public sealed class FacetQueryOptions
{
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

    public string Field { get; }
    public string? SearchTerm { get; }
    public IReadOnlyList<FilterTerm> Filters { get; }
    public SortDirection Direction { get; }
}
