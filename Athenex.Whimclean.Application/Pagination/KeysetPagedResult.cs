namespace Athenex.Whimclean.Application.Pagination;

public sealed class KeysetPagedResult<T, TKey>
    where T : notnull
    where TKey : notnull
{
    public KeysetPagedResult(
        IEnumerable<T> items,
        int size)
        : this(items, size, default, hasNextPage: false)
    {
    }

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

    public IReadOnlyList<T> Items { get; }
    public int Size { get; }
    public bool HasNextPage { get; }
    public TKey NextKey => HasNextPage
        ? _nextKey!
        : throw new InvalidOperationException(
            "The final page does not contain a next key.");
}
