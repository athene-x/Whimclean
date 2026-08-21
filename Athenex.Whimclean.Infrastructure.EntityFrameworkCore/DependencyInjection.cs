using Athenex.Whimclean.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore;

public static class DependencyInjection
{
    public static IServiceCollection AddEntityFrameworkCoreContext<TContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configureDbContext)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        services.AddDbContext<TContext>(configureDbContext);

        return services;
    }

    public static IServiceCollection AddEntityFrameworkCoreDbContext<TContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configureDbContext)
        where TContext : DbContext, IUnitOfWork
    {
        services.AddEntityFrameworkCoreContext<TContext>(configureDbContext);
        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<TContext>());

        return services;
    }
}
