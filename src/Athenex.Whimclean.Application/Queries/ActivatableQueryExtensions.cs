using Athenex.Whimclean.Domain.Behaviors;

namespace Athenex.Whimclean.Application.Queries;

/// <summary>
/// Opt-in filtering for <see cref="IActivatable" /> records. Front-usage
/// queries (selection lists, public listings) call <c>WhereActive()</c>;
/// operational queries omit it and see every record.
/// </summary>
public static class ActivatableQueryExtensions
{
    /// <summary>
    /// Narrows the query to records whose
    /// <see cref="IActivatable.IsActive" /> flag is set; the predicate is
    /// translated into the store query.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="query" /> is <see langword="null" />.
    /// </exception>
    public static IQueryable<T> WhereActive<T>(this IQueryable<T> query)
        where T : IActivatable
    {
        ArgumentNullException.ThrowIfNull(query);

        return query.Where(entity => entity.IsActive);
    }

    /// <summary>
    /// Narrows the in-memory sequence to records whose
    /// <see cref="IActivatable.IsActive" /> flag is set; enumeration is
    /// deferred.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    public static IEnumerable<T> WhereActive<T>(this IEnumerable<T> source)
        where T : IActivatable
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Where(entity => entity.IsActive);
    }
}
