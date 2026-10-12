using System.Text.Json;
using Athenex.Whimclean.Api.AspNetCore.Errors;
using Athenex.Whimclean.Api.AspNetCore.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Api.AspNetCore.Tests.Validation;

public sealed class RequestValidationTests
{
    public sealed class ListThingsRequest : ListQueryRequest;

    public sealed class ListThingsRequestValidator : ListQueryRequestValidator<ListThingsRequest>
    {
        public ListThingsRequestValidator() => AllowSortFields("name");
    }

    private sealed record Echo(string[]? Filters, int? Page, int? Size, string? Sort);

    [Fact]
    public async Task MapApiGroup_BindsListQueryRequestFromQueryString()
    {
        var (status, body) = await InvokeAsync("?filters=status:in:a,b&filters=name:eq:x&page=2&sort=-name");

        Assert.Equal(StatusCodes.Status200OK, status);
        var echo = JsonSerializer.Deserialize<Echo>(body, JsonSerializerOptions.Web);
        Assert.NotNull(echo);
        Assert.Equal(["status:in:a,b", "name:eq:x"], echo.Filters);
        Assert.Equal(2, echo.Page);
        Assert.Equal("-name", echo.Sort);
    }

    [Fact]
    public async Task MapApiGroup_RejectsInvalidRequestWithSharedErrorShape()
    {
        var (status, body) = await InvokeAsync("?sort=email&page=0");

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        var error = JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonSerializerOptions.Web);
        Assert.NotNull(error);
        Assert.Equal(ValidationHttpMapper.ValidationFailedCode, error.Code);
        Assert.NotNull(error.Errors);
        Assert.Contains(error.Errors, detail => detail.Field == "sort");
        Assert.Contains(error.Errors, detail => detail.Field == "page");
    }

    [Fact]
    public async Task MapApiGroup_WithoutRegisteredValidator_RunsHandlerUnchanged()
    {
        var (status, _) = await InvokeAsync("?sort=email", registerValidators: false);

        Assert.Equal(StatusCodes.Status200OK, status);
    }

    [Fact]
    public void MapApiGroup_RejectsBlankPrefix()
    {
        var app = WebApplication.CreateBuilder().Build();

        Assert.Throws<ArgumentException>(() => app.MapApiGroup(" "));
    }

    private static async Task<(int StatusCode, string Body)> InvokeAsync(
        string queryString,
        bool registerValidators = true)
    {
        var builder = WebApplication.CreateBuilder();
        if (registerValidators)
        {
            builder.Services.AddApiValidatorsFromAssemblyContaining<RequestValidationTests>();
        }

        var app = builder.Build();
        app.MapApiGroup().MapGet(
            "things",
            ([AsParameters] ListThingsRequest request) =>
                Results.Ok(new Echo(request.Filters, request.Page, request.Size, request.Sort)));

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText!.EndsWith("things", StringComparison.Ordinal));

        await using var scope = app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Method = HttpMethods.Get;
        context.Request.QueryString = new QueryString(queryString);
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return (context.Response.StatusCode, await reader.ReadToEndAsync());
    }
}
