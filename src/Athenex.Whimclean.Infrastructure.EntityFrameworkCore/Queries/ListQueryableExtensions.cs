using System.Linq.Expressions;
using Athenex.Whimclean.Application.Pagination;
using Athenex.Whimclean.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries;

/// <summary>
/// Query helpers that turn a <see cref="ListQueryOptions" /> into search,
/// paging and page results, so a list query reads as
/// filter → search → count → order → page → project.
/// </summary>
public static class ListQueryableExtensions
{
    private static readonly Expression<Func<string?, string, bool>> LikeMatch =
        (value, pattern) => EF.Functions.Like(value, pattern, LikePattern.EscapeCharacter);

    /// <summary>
    /// Narrows <paramref name="query" /> with the predicate
    /// <paramref name="matches" /> builds for the escaped contains
    /// pattern of <paramref name="searchTerm" />; a
    /// <see langword="null" /> term leaves the query unchanged.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="query" /> or <paramref name="matches" /> is
    /// <see langword="null" />.
    /// </exception>
    public static IQueryable<T> Search<T>(
        this IQueryable<T> query,
        string? searchTerm,
        Func<string, Expression<Func<T, bool>>> matches)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(matches);

        return searchTerm is { } search
            ? query.Where(matches(LikePattern.Contains(search)))
            : query;
    }

    /// <summary>
    /// Narrows <paramref name="query" /> to rows where any of
    /// <paramref name="fields" /> contains <paramref name="searchTerm" />,
    /// using <c>EF.Functions.Like</c> with wildcards escaped. Case
    /// sensitivity follows the column collation; the PostgreSQL package
    /// offers a case-insensitive <c>SearchILike</c>. A
    /// <see langword="null" /> term leaves the query unchanged.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="query" /> or <paramref name="fields" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fields" /> is empty.
    /// </exception>
    public static IQueryable<T> SearchLike<T>(
        this IQueryable<T> query,
        string? searchTerm,
        params Expression<Func<T, string?>>[] fields)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(fields);

        return query.Search(
            searchTerm,
            pattern => SearchPredicate.AnyMatch(LikeMatch, pattern, fields));
    }

    /// <summary>
    /// Applies the page's offset and size; call after ordering so the
    /// page is deterministic.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="query" /> or <paramref name="options" /> is
    /// <see langword="null" />.
    /// </exception>
    public static IQueryable<T> Page<T>(
        this IOrderedQueryable<T> query,
        ListQueryOptions options)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);

        return query.Skip(options.Offset).Take(options.Size);
    }

    /// <summary>
    /// Wraps the materialized page items with the page number and size
    /// from <paramref name="options" /> and the overall
    /// <paramref name="totalItems" />.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items" /> or <paramref name="options" /> is
    /// <see langword="null" />.
    /// </exception>
    public static OffsetPagedResult<T> ToPage<T>(
        this IReadOnlyCollection<T> items,
        ListQueryOptions options,
        long totalItems)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(options);

        return new OffsetPagedResult<T>(items, options.Page, options.Size, totalItems);
    }
}
