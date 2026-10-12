using System.Security.Claims;
using Athenex.Whimclean.Api.AspNetCore.Context;
using Athenex.Whimclean.Application.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Api.AspNetCore.Tests.Context;

public sealed class LocalUserResolutionTests
{
    private sealed class DictionaryLookup(Dictionary<string, Guid> users) : ILocalUserLookup<Guid>
    {
        public List<string> Subjects { get; } = [];

        public ValueTask<Guid> FindByExternalSubjectAsync(
            string externalSubject,
            CancellationToken cancellationToken = default)
        {
            Subjects.Add(externalSubject);
            return ValueTask.FromResult(users.GetValueOrDefault(externalSubject));
        }
    }

    private static DefaultHttpContext AuthenticatedContext(string? subject)
    {
        var claims = subject is null ? [] : new[] { new Claim("sub", subject) };
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
    }

    [Fact]
    public async Task Middleware_SetsTheLocalUserForAKnownSubject()
    {
        var localUserId = Guid.NewGuid();
        var lookup = new DictionaryLookup(new() { ["external-1"] = localUserId });
        var currentUser = new RequestCurrentUser<Guid>();
        var reachedNext = false;
        var middleware = new LocalUserResolutionMiddleware<Guid>(_ =>
        {
            reachedNext = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(
            AuthenticatedContext("external-1"),
            Options.Create(new ClaimsCurrentUserOptions { ClaimType = "sub" }),
            lookup,
            currentUser);

        Assert.True(reachedNext);
        Assert.True(currentUser.TryGetUserId(out var userId));
        Assert.Equal(localUserId, userId);
    }

    [Fact]
    public async Task Middleware_LeavesTheUserUnsetForUnknownOrAnonymousCallers()
    {
        var lookup = new DictionaryLookup([]);
        var currentUser = new RequestCurrentUser<Guid>();
        var middleware = new LocalUserResolutionMiddleware<Guid>(_ => Task.CompletedTask);
        var options = Options.Create(new ClaimsCurrentUserOptions { ClaimType = "sub" });

        await middleware.InvokeAsync(AuthenticatedContext("unknown"), options, lookup, currentUser);
        await middleware.InvokeAsync(new DefaultHttpContext(), options, lookup, currentUser);

        Assert.False(currentUser.TryGetUserId(out _));
        Assert.Equal(["unknown"], lookup.Subjects);
    }

    [Fact]
    public void RequestCurrentUser_RejectsDefaultIdentifier()
    {
        var currentUser = new RequestCurrentUser<Guid>();

        Assert.Throws<ArgumentException>(() => currentUser.SetUserId(Guid.Empty));
        Assert.False(currentUser.TryGetUserId(out _));
    }

    [Fact]
    public void AddLocalUserResolution_ExposesOneScopedInstanceUnderBothContracts()
    {
        var services = new ServiceCollection();
        services.AddLocalUserResolution<Guid>(options => options.ClaimType = "sub");
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var initializer = scope.ServiceProvider.GetRequiredService<ICurrentUserInitializer<Guid>>();
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser<Guid>>();
        var userId = Guid.NewGuid();
        initializer.SetUserId(userId);

        Assert.True(currentUser.TryGetUserId(out var resolved));
        Assert.Equal(userId, resolved);
        Assert.Equal(
            "sub",
            scope.ServiceProvider.GetRequiredService<IOptions<ClaimsCurrentUserOptions>>().Value.ClaimType);
    }
}
