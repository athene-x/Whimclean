using System.Security.Claims;
using Athenex.Whimclean.Api.AspNetCore.Context;
using Athenex.Whimclean.Application.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Api.AspNetCore.Tests.Context;

public sealed class ClaimsCurrentUserTests
{
    [Fact]
    public void TryGetUserId_WithAuthenticatedPrincipal_ParsesTheClaim()
    {
        var userId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(
            CreatePrincipal(ClaimTypes.NameIdentifier, userId.ToString()));

        Assert.True(currentUser.TryGetUserId(out var resolvedUserId));
        Assert.Equal(userId, resolvedUserId);
    }

    [Fact]
    public void TryGetUserId_WithoutRequest_ResolvesNothing()
    {
        var currentUser = CreateCurrentUser(httpContext: null);

        Assert.False(currentUser.TryGetUserId(out _));
    }

    [Fact]
    public void TryGetUserId_WithUnauthenticatedPrincipal_ResolvesNothing()
    {
        // An anonymous principal may still carry claims; only an
        // authenticated identity attributes writes.
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())]));
        var currentUser = CreateCurrentUser(principal);

        Assert.False(currentUser.TryGetUserId(out _));
    }

    [Fact]
    public void TryGetUserId_WithoutTheClaim_ResolvesNothing()
    {
        var currentUser = CreateCurrentUser(
            CreatePrincipal(ClaimTypes.Email, "member@example.com"));

        Assert.False(currentUser.TryGetUserId(out _));
    }

    [Fact]
    public void TryGetUserId_WithUnparsableClaim_ResolvesNothing()
    {
        var currentUser = CreateCurrentUser(
            CreatePrincipal(ClaimTypes.NameIdentifier, "not-a-guid"));

        Assert.False(currentUser.TryGetUserId(out _));
    }

    [Fact]
    public void TryGetUserId_ReadsTheConfiguredClaimType()
    {
        var userId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(
            CreatePrincipal("sub", userId.ToString()),
            options => options.ClaimType = "sub");

        Assert.True(currentUser.TryGetUserId(out var resolvedUserId));
        Assert.Equal(userId, resolvedUserId);
    }

    [Fact]
    public void AddClaimsCurrentUser_RegistersTheScopedImplementation()
    {
        var services = new ServiceCollection();
        services.AddClaimsCurrentUser<Guid>();

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        Assert.IsType<ClaimsCurrentUser<Guid>>(
            scope.ServiceProvider.GetRequiredService<ICurrentUser<Guid>>());
    }

    [Fact]
    public void AddClaimsCurrentUser_LeavesAnExistingRegistrationInPlace()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICurrentUser<Guid>, SystemCurrentUser>();
        services.AddClaimsCurrentUser<Guid>();

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        Assert.IsType<SystemCurrentUser>(
            scope.ServiceProvider.GetRequiredService<ICurrentUser<Guid>>());
    }

    private static ClaimsCurrentUser<Guid> CreateCurrentUser(
        ClaimsPrincipal principal,
        Action<ClaimsCurrentUserOptions>? configure = null) =>
        CreateCurrentUser(new DefaultHttpContext { User = principal }, configure);

    private static ClaimsCurrentUser<Guid> CreateCurrentUser(
        HttpContext? httpContext,
        Action<ClaimsCurrentUserOptions>? configure = null)
    {
        var options = new ClaimsCurrentUserOptions();
        configure?.Invoke(options);

        return new ClaimsCurrentUser<Guid>(
            new FixedHttpContextAccessor(httpContext),
            Options.Create(options));
    }

    private static ClaimsPrincipal CreatePrincipal(
        string claimType,
        string claimValue) =>
        new(new ClaimsIdentity(
            [new Claim(claimType, claimValue)],
            authenticationType: "Test"));

    private sealed class FixedHttpContextAccessor(HttpContext? httpContext)
        : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = httpContext;
    }

    private sealed class SystemCurrentUser : ICurrentUser<Guid>
    {
        public bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;
            return true;
        }
    }
}
