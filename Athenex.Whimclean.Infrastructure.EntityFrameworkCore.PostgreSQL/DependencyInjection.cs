using Athenex.Whimclean.Application.Persistence;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL;

public static class DependencyInjection
{
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

    public static IServiceCollection AddPostgreSqlDbContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? configurePostgreSql = null)
        where TContext : DbContext, IUnitOfWork
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return services.AddEntityFrameworkCoreDbContext<TContext>((_, options) =>
            options.UseNpgsql(connectionString, postgreSqlOptions =>
                configurePostgreSql?.Invoke(postgreSqlOptions)));
    }
}
