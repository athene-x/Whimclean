using Athenex.Whimclean.Api.AspNetCore.Operations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Athenex.Whimclean.Api.AspNetCore.Tests.Operations;

public sealed class HealthEndpointsTests
{
    [Fact]
    public void MapHealthEndpoints_MapsLiveAndReadyRoutes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthChecks()
            .AddCheck("ready-check", () => HealthCheckResult.Healthy(), tags: [HealthCheckTags.Ready]);
        var app = builder.Build();

        app.MapHealthEndpoints();

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToArray();

        Assert.Contains(HealthEndpoints.DefaultLivePath, routes);
        Assert.Contains(HealthEndpoints.DefaultReadyPath, routes);
    }

    [Fact]
    public void MapHealthEndpoints_RejectsBlankPaths()
    {
        var app = WebApplication.CreateBuilder().Build();

        Assert.Throws<ArgumentException>(() => app.MapHealthEndpoints(livePath: " "));
    }
}
