using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Auditing.AspNetCore.Tests;

public sealed class HttpAuditRequestContextTests
{
    [Fact]
    public void ReadsActorCorrelationAddressAndAgentFromTheRequest()
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("email", "a@example.com")], "test"))
        };
        http.Request.Headers["X-Request-Id"] = "req-1";
        http.Request.Headers["User-Agent"] = "agent/1";
        http.Connection.RemoteIpAddress = IPAddress.Parse("10.1.2.3");
        var context = new HttpAuditRequestContext(new HttpContextAccessor { HttpContext = http });

        Assert.Equal("a@example.com", context.ActorName);
        Assert.Equal("req-1", context.CorrelationId);
        Assert.Equal("10.1.2.3", context.IpAddress);
        Assert.Equal("agent/1", context.UserAgent);
    }

    [Fact]
    public void PrefersTheNameClaimAndCorrelationHeader()
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("name", "Alice"), new Claim("email", "a@example.com")],
                "test"))
        };
        http.Request.Headers["X-Correlation-Id"] = "corr-1";
        http.Request.Headers["X-Request-Id"] = "req-1";
        var context = new HttpAuditRequestContext(new HttpContextAccessor { HttpContext = http });

        Assert.Equal("Alice", context.ActorName);
        Assert.Equal("corr-1", context.CorrelationId);
    }

    [Fact]
    public void FallsBackToTheTraceIdentifierAndNoActorWhenAnonymous()
    {
        var http = new DefaultHttpContext { TraceIdentifier = "trace-1" };
        var context = new HttpAuditRequestContext(new HttpContextAccessor { HttpContext = http });

        Assert.Null(context.ActorName);
        Assert.Equal("trace-1", context.CorrelationId);
    }

    [Fact]
    public void IsEmptyOutsideARequest()
    {
        var context = new HttpAuditRequestContext(new HttpContextAccessor { HttpContext = null });

        Assert.Null(context.ActorName);
        Assert.Null(context.CorrelationId);
        Assert.Null(context.IpAddress);
        Assert.Null(context.UserAgent);
    }

    [Fact]
    public void AddHttpAuditRequestContext_ReplacesTheNullContext()
    {
        var services = new ServiceCollection();
        services.AddAuditing<Guid, Guid>();

        services.AddHttpAuditRequestContext();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IAuditRequestContext));
        Assert.Equal(typeof(HttpAuditRequestContext), descriptor.ImplementationType);
    }
}
