using System.Net;
using System.Security.Claims;
using Athenex.Whimclean.Api.AspNetCore.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Api.AspNetCore.Tests.Operations;

public sealed class RequestRateLimitingTests
{
    [Fact]
    public void PartitionKey_UsesSubjectWhenAuthenticatedOtherwiseAddress()
    {
        var authenticated = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "abc")], "test"))
        };
        var anonymous = new DefaultHttpContext();
        anonymous.Connection.RemoteIpAddress = IPAddress.Loopback;

        Assert.Equal("user:abc", RequestRateLimiting.PartitionKey(authenticated));
        Assert.Equal("ip:127.0.0.1", RequestRateLimiting.PartitionKey(anonymous));
    }

    [Fact]
    public void AddRequestRateLimiting_BindsTheConfigurationSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "5",
                ["RateLimiting:WindowSeconds"] = "1"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddRequestRateLimiting(configuration);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType.FullName == "Microsoft.Extensions.Options.IConfigureOptions`1[[Microsoft.AspNetCore.RateLimiting.RateLimiterOptions, Microsoft.AspNetCore.RateLimiting, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60]]");
    }

    [Fact]
    public void AddRequestRateLimiting_RejectsNonPositiveLimits()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddRequestRateLimiting(new RequestRateLimitOptions { PermitLimit = 0 }));
        Assert.Throws<InvalidOperationException>(() =>
            services.AddRequestRateLimiting(new RequestRateLimitOptions { WindowSeconds = -1 }));
    }
}
