namespace Athenex.Whimclean.Application.Pagination;

/// <summary>
/// Projection helpers for <see cref="OffsetPagedResult{T}" />.
/// </summary>
public static class OffsetPagedResultExtensions
{
    /// <summary>
    /// Projects every item of the page with <paramref name="map" />,
    /// keeping the page number, size and totals; typically used to turn
    /// persistence projections into API response models.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="page" /> or <paramref name="map" /> is
    /// <see langword="null" />.
    /// </exception>
    public static OffsetPagedResult<TResult> Map<T, TResult>(
        this OffsetPagedResult<T> page,
        Func<T, TResult> map)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(map);

        return new OffsetPagedResult<TResult>(
            page.Items.Select(map),
            page.Page,
            page.Size,
            page.TotalItems);
    }
}
