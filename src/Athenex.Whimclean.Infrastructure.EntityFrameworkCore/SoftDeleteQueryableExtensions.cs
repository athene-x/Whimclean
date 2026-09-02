using Microsoft.EntityFrameworkCore;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore;

/// <summary>
/// Query extensions for working around the global soft-delete filter
/// applied by the library's DbContexts.
/// </summary>
public static class SoftDeleteQueryableExtensions
{
    /// <summary>
    /// Disables only the soft-delete query filter for this query, so
    /// soft-deleted records (including ones reached through Include)
    /// become visible — e.g. an archived document that still references
    /// a deleted item. Every other filter, notably tenant isolation,
    /// stays active, unlike bare <c>IgnoreQueryFilters()</c>.
    /// </summary>
    public static IQueryable<T> IncludeSoftDeleted<T>(
        this IQueryable<T> query)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(query);

        return query.IgnoreQueryFilters([QueryFilterNames.SoftDelete]);
    }
}
