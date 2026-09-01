using Athenex.Whimclean.Infrastructure.AspNetCoreIdentity.EntityFrameworkCore.Persistence;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Infrastructure.AspNetCoreIdentity.EntityFrameworkCore.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public async Task AddEntityFrameworkCoreIdentity_RegistersWorkingManagersAndStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEntityFrameworkCoreContext<TestAuthenticationDbContext>(
            (_, options) =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddEntityFrameworkCoreIdentity<
            TestUser,
            IdentityRole<Guid>,
            TestAuthenticationDbContext>(options =>
                options.User.RequireUniqueEmail = true);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<TestUser>>();
        var signInManager = scope.ServiceProvider
            .GetRequiredService<SignInManager<TestUser>>();
        var user = new TestUser
        {
            Id = Guid.NewGuid(),
            UserName = "member@example.com",
            Email = "member@example.com"
        };

        var creation = await userManager.CreateAsync(
            user,
            "Example123!");

        Assert.True(creation.Succeeded);
        Assert.True(userManager.Options.User.RequireUniqueEmail);
        Assert.NotNull(signInManager);
        Assert.NotNull(await userManager.FindByIdAsync(user.Id.ToString()));
    }

    private sealed class TestAuthenticationDbContext(
        DbContextOptions<TestAuthenticationDbContext> options)
        : IdentityAuthenticationDbContext<
            TestUser,
            IdentityRole<Guid>,
            Guid>(options);

    private sealed class TestUser : IdentityUser<Guid>;
}
