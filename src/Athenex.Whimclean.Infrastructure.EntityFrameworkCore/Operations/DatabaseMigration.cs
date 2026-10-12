using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Operations;

/// <summary>
/// Lets the API image apply its own migrations: a deployment runs the
/// same container once with <c>--migrate</c> before starting the
/// application, instead of shipping the EF tooling to the server.
/// </summary>
public static class DatabaseMigration
{
    /// <summary>Command-line switch that requests a migration run.</summary>
    public const string CommandLineSwitch = "--migrate";

    /// <summary>
    /// Whether <paramref name="args"/> contain
    /// <see cref="CommandLineSwitch"/> (case-insensitive).
    /// </summary>
    public static bool IsRequested(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        return args.Contains(CommandLineSwitch, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Applies the pending migrations of <typeparamref name="TContext"/>
    /// in a new service scope, logging which ones ran.
    /// </summary>
    /// <returns>The number of migrations applied.</returns>
    public static async Task<int> MigrateDatabaseAsync<TContext>(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var logger = scope.ServiceProvider
            .GetService<ILoggerFactory>()?
            .CreateLogger(typeof(DatabaseMigration));
        var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken))
            .ToArray();

        if (pending.Length == 0)
        {
            logger?.LogInformation("The database is up to date.");
            return 0;
        }

        logger?.LogInformation(
            "Applying {Count} migration(s): {Migrations}",
            pending.Length,
            string.Join(", ", pending));

        await dbContext.Database.MigrateAsync(cancellationToken);

        return pending.Length;
    }
}
