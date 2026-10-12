using System.Net;
using System.Net.Http.Json;
using Athenex.Whimclean.Api.AspNetCore.Errors;
using Athenex.Whimclean.Api.AspNetCore.Queries;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Api.FastEndpoints.Tests;

public sealed class ListThingsRequest : ListQueryRequest;

// Derives from the primary package's validator (a plain FluentValidation
// AbstractValidator); FastEndpoints must still discover it on its own.
public sealed class ListThingsRequestValidator : ListQueryRequestValidator<ListThingsRequest>
{
    public ListThingsRequestValidator() => AllowSortFields("name");
}

public sealed record ListThingsResponse(string[] Filters, int? Page, string? Sort);

public sealed class ListThingsEndpoint : Endpoint<ListThingsRequest, ListThingsResponse>
{
    public override void Configure()
    {
        Get("things");
        AllowAnonymous();
    }

    public override Task HandleAsync(ListThingsRequest request, CancellationToken ct) =>
        Send.OkAsync(new ListThingsResponse(request.Filters ?? [], request.Page, request.Sort), ct);
}

public sealed class FastEndpointsAdapterTests
{
    [Fact]
    public async Task UseFastEndpointsApi_BindsSharedListQueryRequestAndValidatesIt()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddFastEndpointsApi();
        var app = builder.Build();
        app.UseFastEndpointsApi();
        await app.StartAsync();

        try
        {
            using var client = new HttpClient
            {
                BaseAddress = new Uri(app.Services
                    .GetRequiredService<IServer>()
                    .Features.Get<IServerAddressesFeature>()!
                    .Addresses.First())
            };

            var ok = await client.GetAsync("api/v1/things?filters=status:in:a,b&page=3&sort=-name");
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            var payload = await ok.Content.ReadFromJsonAsync<ListThingsResponse>();
            Assert.NotNull(payload);
            Assert.Equal(["status:in:a,b"], payload.Filters);
            Assert.Equal(3, payload.Page);
            Assert.Equal("-name", payload.Sort);

            var invalid = await client.GetAsync("api/v1/things?sort=email");
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            var error = await invalid.Content.ReadFromJsonAsync<ApiErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal(ValidationHttpMapper.ValidationFailedCode, error.Code);
            Assert.NotNull(error.Errors);
            var detail = Assert.Single(error.Errors);
            Assert.Equal("sort", detail.Field);
            Assert.Equal("Sort field 'email' is not supported.", detail.Description);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
