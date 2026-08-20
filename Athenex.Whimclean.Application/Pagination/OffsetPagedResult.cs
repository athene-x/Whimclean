namespace Athenex.Whimclean.Application.Pagination;

public sealed class OffsetPagedResult<T>
    where T : notnull
{
    public OffsetPagedResult(
        IEnumerable<T> items,
        int page,
        int size,
        long totalItems)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(totalItems);

        var materializedItems = items.ToArray();

        if (materializedItems.Length > size)
        {
            throw new ArgumentException(
                "The item count cannot exceed the page size.",
                nameof(items));
        }

        if (materializedItems.LongLength > totalItems)
        {
            throw new ArgumentException(
                "The item count cannot exceed the total item count.",
                nameof(totalItems));
        }

        Items = Array.AsReadOnly(materializedItems);
        Page = page;
        Size = size;
        TotalItems = totalItems;
    }

    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int Size { get; }
    public long TotalItems { get; }
    public long TotalPages =>
        (TotalItems / Size) + (TotalItems % Size == 0 ? 0 : 1);
}
