using System.Collections;
using System.Linq.Expressions;
using Athenex.Whimclean.Application.Queries;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries;

/// <summary>
/// Maps the sort field names an endpoint accepts to key selectors, so a
/// list query's <see cref="SortTerm" />s can be applied without a switch
/// per field and direction. Declare one per entity with a collection
/// initializer and keep it static; names are compared ordinally.
/// </summary>
/// <typeparam name="T">Element type of the queries being ordered.</typeparam>
public sealed class SortFields<T> : IEnumerable<string>
{
    private readonly Dictionary<string, Field> _fields = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers <paramref name="name" /> as sortable by
    /// <paramref name="key" />.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="name" /> is <see langword="null" /> or whitespace,
    /// or was already added.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public void Add<TKey>(string name, Expression<Func<T, TKey>> key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(key);

        _fields.Add(name, new Field(
            (query, descending) => descending ? query.OrderByDescending(key) : query.OrderBy(key),
            (query, descending) => descending ? query.ThenByDescending(key) : query.ThenBy(key)));
    }

    /// <summary>
    /// Whether <paramref name="name" /> is a registered sort field.
    /// </summary>
    public bool Contains(string name) => _fields.ContainsKey(name);

    /// <summary>
    /// Orders <paramref name="query" /> by the registered fields named in
    /// <paramref name="terms" />, in sequence; unknown names are skipped.
    /// </summary>
    /// <returns>
    /// The ordered query, or <see langword="null" /> when no term named a
    /// registered field, so the caller can apply its default order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="query" /> or <paramref name="terms" /> is
    /// <see langword="null" />.
    /// </exception>
    public IOrderedQueryable<T>? Order(IQueryable<T> query, IEnumerable<SortTerm> terms)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(terms);

        IOrderedQueryable<T>? ordered = null;

        foreach (var term in terms)
        {
            if (!_fields.TryGetValue(term.Field, out var field))
            {
                continue;
            }

            var descending = term.Direction == SortDirection.Descending;

            ordered = ordered is null
                ? field.OrderBy(query, descending)
                : field.ThenBy(ordered, descending);
        }

        return ordered;
    }

    /// <summary>
    /// Orders <paramref name="query" /> by the registered fields named in
    /// <paramref name="terms" />, falling back to
    /// <paramref name="defaultKey" /> when none applies, and always ends
    /// with <paramref name="tieBreaker" /> ascending so pages are stable.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Any argument is <see langword="null" />.
    /// </exception>
    public IOrderedQueryable<T> Order<TDefault, TTieBreaker>(
        IQueryable<T> query,
        IEnumerable<SortTerm> terms,
        Expression<Func<T, TDefault>> defaultKey,
        Expression<Func<T, TTieBreaker>> tieBreaker,
        SortDirection defaultDirection = SortDirection.Ascending)
    {
        ArgumentNullException.ThrowIfNull(defaultKey);
        ArgumentNullException.ThrowIfNull(tieBreaker);

        var ordered = Order(query, terms)
            ?? (defaultDirection == SortDirection.Descending
                ? query.OrderByDescending(defaultKey)
                : query.OrderBy(defaultKey));

        return ordered.ThenBy(tieBreaker);
    }

    /// <summary>Enumerates the registered field names.</summary>
    public IEnumerator<string> GetEnumerator() => _fields.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed record Field(
        Func<IQueryable<T>, bool, IOrderedQueryable<T>> OrderBy,
        Func<IOrderedQueryable<T>, bool, IOrderedQueryable<T>> ThenBy);
}
