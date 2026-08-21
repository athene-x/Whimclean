using Athenex.Whimclean.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddPostgreSqlContext_RegistersContextWithoutUnitOfWork()
    {
        var services = new ServiceCollection();
        services.AddPostgreSqlContext<PlainDbContext>(
            "Host=localhost;Database=test;Username=test;Password=test");

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<PlainDbContext>();

        Assert.Equal(
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            context.Database.ProviderName);
        Assert.Null(scope.ServiceProvider.GetService<IUnitOfWork>());
    }

    [Fact]
    public void AddPostgreSqlDbContext_RegistersNpgsqlContextAndUnitOfWork()
    {
        var services = new ServiceCollection();
        services.AddPostgreSqlDbContext<TestDbContext>(
            "Host=localhost;Database=test;Username=test;Password=test");

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Assert.Equal(
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            context.Database.ProviderName);
        Assert.Same(context, unitOfWork);
    }

    private sealed class TestDbContext(
        DbContextOptions<TestDbContext> options)
        : DbContext(options), IUnitOfWork
    {
        public Task CommitAsync(
            CancellationToken cancellationToken = default) =>
            SaveChangesAsync(cancellationToken);
    }

    private sealed class PlainDbContext(
        DbContextOptions<PlainDbContext> options)
        : DbContext(options);
}
