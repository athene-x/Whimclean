using Athenex.Whimclean.Application.Persistence;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL;

/// <summary>
/// Service registrations for EF Core persistence backed by PostgreSQL via
/// Npgsql.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers <typeparamref name="TContext" /> as a scoped DbContext
    /// using <c>UseNpgsql</c> with the given connection string; nothing
    /// else is registered. Use
    /// <see cref="AddPostgreSqlUnitOfWork{TContext}" /> to also expose the
    /// context as <see cref="IUnitOfWork" />.
    /// </summary>
    /// <typeparam name="TContext">Concrete context to register.</typeparam>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="connectionString" /> is <see langword="null" /> or
    /// whitespace.
    /// </exception>
    public static IServiceCollection AddPostgreSqlContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? configurePostgreSql = null)
        where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return services.AddEntityFrameworkCoreContext<TContext>((_, options) =>
            options.UseNpgsql(connectionString, postgreSqlOptions =>
                configurePostgreSql?.Invoke(postgreSqlOptions)));
    }

    /// <summary>
    /// Registers <typeparamref name="TContext" /> as a scoped DbContext
    /// using <c>UseNpgsql</c> with the given connection string and
    /// additionally exposes the same scoped instance as
    /// <see cref="IUnitOfWork" />.
    /// </summary>
    /// <typeparam name="TContext">Concrete context to register.</typeparam>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="connectionString" /> is <see langword="null" /> or
    /// whitespace.
    /// </exception>
    public static IServiceCollection AddPostgreSqlUnitOfWork<TContext>(
        this IServiceCollection services,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? configurePostgreSql = null)
        where TContext : DbContext, IUnitOfWork
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return services.AddEntityFrameworkCoreUnitOfWork<TContext>((_, options) =>
            options.UseNpgsql(connectionString, postgreSqlOptions =>
                configurePostgreSql?.Invoke(postgreSqlOptions)));
    }
}
