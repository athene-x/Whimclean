namespace Athenex.Whimclean.Application.Pagination;

/// <summary>
/// One page of a keyset (cursor) paginated query. A page is either the
/// final one or carries the non-null key the next page starts from; which
/// of the two is fixed by the constructor used. The item count can never
/// exceed <see cref="Size" />.
/// </summary>
/// <typeparam name="T">Type of the page's items.</typeparam>
/// <typeparam name="TKey">Type of the continuation key.</typeparam>
public sealed class KeysetPagedResult<T, TKey>
    where T : notnull
    where TKey : notnull
{
    /// <summary>
    /// Creates the final page: <see cref="HasNextPage" /> is
    /// <see langword="false" /> and <see cref="NextKey" /> is unavailable.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="size" /> is less than 1.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="items" /> holds more than <paramref name="size" />
    /// elements or contains a <see langword="null" /> element.
    /// </exception>
    public KeysetPagedResult(
        IEnumerable<T> items,
        int size)
        : this(items, size, default, hasNextPage: false)
    {
    }

    /// <summary>
    /// Creates a page that is followed by at least one more:
    /// <see cref="HasNextPage" /> is <see langword="true" /> and
    /// <paramref name="nextKey" /> becomes <see cref="NextKey" />.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items" /> or <paramref name="nextKey" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="size" /> is less than 1.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="items" /> holds more than <paramref name="size" />
    /// elements or contains a <see langword="null" /> element.
    /// </exception>
    public KeysetPagedResult(
        IEnumerable<T> items,
        int size,
        TKey nextKey)
        : this(items, size, nextKey, hasNextPage: true)
    {
        ArgumentNullException.ThrowIfNull(nextKey);
    }

    private KeysetPagedResult(
        IEnumerable<T> items,
        int size,
        TKey? nextKey,
        bool hasNextPage)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);

        var materializedItems = items.ToArray();

        if (materializedItems.Any(item => item is null))
        {
            throw new ArgumentException(
                "The items cannot contain a null element.",
                nameof(items));
        }

        if (materializedItems.Length > size)
        {
            throw new ArgumentException(
                "The item count cannot exceed the page size.",
                nameof(items));
        }

        Items = Array.AsReadOnly(materializedItems);
        Size = size;
        _nextKey = nextKey;
        HasNextPage = hasNextPage;
    }

    private readonly TKey? _nextKey;

    /// <summary>
    /// Read-only items on this page; never more than <see cref="Size" />
    /// of them.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Requested page size; always at least 1.</summary>
    public int Size { get; }

    /// <summary>
    /// Whether a further page exists, i.e. whether <see cref="NextKey" />
    /// may be read.
    /// </summary>
    public bool HasNextPage { get; }

    /// <summary>
    /// Key the next page starts from; never <see langword="null" />.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// This is the final page (<see cref="HasNextPage" /> is
    /// <see langword="false" />), which has no next key.
    /// </exception>
    public TKey NextKey => HasNextPage
        ? _nextKey!
        : throw new InvalidOperationException(
            "The final page does not contain a next key.");
}
