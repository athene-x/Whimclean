namespace Athenex.Whimclean.Application.Pagination;

/// <summary>
/// One page of an offset-paginated query together with the overall totals.
/// Construction enforces the invariants: the item count can exceed neither
/// the page size nor the total item count.
/// </summary>
/// <typeparam name="T">Type of the page's items.</typeparam>
public sealed class OffsetPagedResult<T>
    where T : notnull
{
    /// <summary>
    /// Creates the page after validating its invariants.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="page" /> or <paramref name="size" /> is less than 1,
    /// or <paramref name="totalItems" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="items" /> holds more than <paramref name="size" />
    /// or more than <paramref name="totalItems" /> elements.
    /// </exception>
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

    /// <summary>
    /// Read-only items on this page; never more than <see cref="Size" />
    /// of them.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>One-based number of this page; always at least 1.</summary>
    public int Page { get; }

    /// <summary>Requested page size; always at least 1.</summary>
    public int Size { get; }

    /// <summary>
    /// Number of items across all pages of the query; never negative.
    /// </summary>
    public long TotalItems { get; }

    /// <summary>
    /// Number of pages needed to hold <see cref="TotalItems" /> items at
    /// <see cref="Size" /> per page (rounded up); 0 for an empty result.
    /// </summary>
    public long TotalPages =>
        (TotalItems / Size) + (TotalItems % Size == 0 ? 0 : 1);
}
