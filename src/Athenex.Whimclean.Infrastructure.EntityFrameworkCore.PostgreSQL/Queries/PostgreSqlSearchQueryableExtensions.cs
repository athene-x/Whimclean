using System.Linq.Expressions;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries;
using Microsoft.EntityFrameworkCore;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL.Queries;

/// <summary>
/// Case-insensitive search helpers for PostgreSQL list queries.
/// </summary>
public static class PostgreSqlSearchQueryableExtensions
{
    private static readonly Expression<Func<string?, string, bool>> ILikeMatch =
        (value, pattern) => EF.Functions.ILike(value!, pattern, LikePattern.EscapeCharacter);

    /// <summary>
    /// Narrows <paramref name="query" /> to rows where any of
    /// <paramref name="fields" /> contains <paramref name="searchTerm" />
    /// regardless of case, using <c>ILIKE</c> with wildcards escaped. A
    /// <see langword="null" /> term leaves the query unchanged.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="query" /> or <paramref name="fields" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fields" /> is empty.
    /// </exception>
    public static IQueryable<T> SearchILike<T>(
        this IQueryable<T> query,
        string? searchTerm,
        params Expression<Func<T, string?>>[] fields)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(fields);

        return query.Search(
            searchTerm,
            pattern => SearchPredicate.AnyMatch(ILikeMatch, pattern, fields));
    }
}
