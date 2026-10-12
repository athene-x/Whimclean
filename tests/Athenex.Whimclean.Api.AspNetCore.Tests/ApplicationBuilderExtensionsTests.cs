using System.Text.Json;
using Athenex.Whimclean.Api.AspNetCore.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Api.AspNetCore.Tests;

public sealed class ApplicationBuilderExtensionsTests
{
    [Fact]
    public void UseMinimalApi_MapsOpenApiDocument()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddMinimalApi();
        var app = builder.Build();

        app.UseMinimalApi();

        Assert.Contains(
            ApplicationBuilderExtensions.DefaultOpenApiRoutePattern,
            MappedRoutes(app));
    }

    [Fact]
    public void UseMinimalApi_WithNullPattern_LeavesOpenApiUnmapped()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddMinimalApi();
        var app = builder.Build();

        app.UseMinimalApi(openApiRoutePattern: null);

        Assert.DoesNotContain(
            ApplicationBuilderExtensions.DefaultOpenApiRoutePattern,
            MappedRoutes(app));
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest, "Request.Invalid")]
    [InlineData(StatusCodes.Status401Unauthorized, "Authentication.Unauthorized")]
    [InlineData(StatusCodes.Status403Forbidden, "Authorization.Forbidden")]
    [InlineData(StatusCodes.Status404NotFound, "Resource.NotFound")]
    public async Task UseApiErrorHandling_GivesEmptyErrorResponsesABody(int statusCode, string expectedCode)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddApiErrorHandling();
        var app = builder.Build();
        app.UseApiErrorHandling();
        app.Run(context =>
        {
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        });
        var pipeline = ((IApplicationBuilder)app).Build();

        await using var scope = app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Response.Body = new MemoryStream();

        await pipeline(context);

        context.Response.Body.Position = 0;
        var error = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            context.Response.Body,
            JsonSerializerOptions.Web);
        Assert.Equal(statusCode, context.Response.StatusCode);
        Assert.Equal(expectedCode, Assert.IsType<ApiErrorResponse>(error).Code);
    }

    private static string[] MappedRoutes(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText!)
            .ToArray();
}
