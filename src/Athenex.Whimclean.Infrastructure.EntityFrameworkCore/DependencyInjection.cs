using Athenex.Whimclean.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore;

/// <summary>
/// Service registrations for EF Core-backed persistence.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers <typeparamref name="TContext" /> as a scoped DbContext
    /// configured by <paramref name="configureDbContext" />; nothing else
    /// is registered. Use
    /// <see cref="AddEntityFrameworkCoreUnitOfWork{TContext}" /> to also
    /// expose the context as <see cref="IUnitOfWork" />.
    /// </summary>
    /// <typeparam name="TContext">Concrete context to register.</typeparam>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configureDbContext" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddEntityFrameworkCoreContext<TContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configureDbContext)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        services.AddDbContext<TContext>(configureDbContext);

        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TContext" /> as a scoped DbContext
    /// and additionally exposes the same scoped instance as
    /// <see cref="IUnitOfWork" />, so application code can commit without
    /// depending on EF Core.
    /// </summary>
    /// <typeparam name="TContext">Concrete context to register.</typeparam>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configureDbContext" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddEntityFrameworkCoreUnitOfWork<TContext>(
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
