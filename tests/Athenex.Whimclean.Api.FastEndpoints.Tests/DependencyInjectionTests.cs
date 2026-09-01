using Athenex.Whimclean.Api.AspNetCore.Errors;
using FastEndpoints;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Api.FastEndpoints.Tests;

// FastEndpoints discovery requires at least one endpoint in the scanned
// assemblies; this one exists so AddFastEndpointsApi can run in tests.
public sealed class PingEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("ping");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct) =>
        Send.OkAsync(cancellation: ct);
}

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddFastEndpointsApi_RegistersSharedErrorHandling()
    {
        var services = new ServiceCollection();

        services.AddFastEndpointsApi();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IApiExceptionMapper) &&
            descriptor.ImplementationType == typeof(DefaultApiExceptionMapper));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IExceptionHandler) &&
            descriptor.ImplementationType == typeof(ApiExceptionHandler));
    }
}
