using System.Text.Json;
using Athenex.Whimclean.Api.AspNetCore.Errors;
using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Application.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Athenex.Whimclean.Api.AspNetCore.Tests.Errors;

public sealed class ApiExceptionHandlerTests
{
    public static TheoryData<Exception, int, string> KnownExceptions => new()
    {
        {
            new CurrentUserRequiredException(),
            StatusCodes.Status401Unauthorized,
            "Authentication.CurrentUserRequired"
        },
        {
            new PersistenceConcurrencyException(
                "Concurrent update.",
                new InvalidOperationException()),
            StatusCodes.Status409Conflict,
            "Persistence.ConcurrentUpdate"
        }
    };

    [Theory]
    [MemberData(nameof(KnownExceptions))]
    public async Task TryHandleAsync_MapsKnownException(
        Exception exception,
        int expectedStatusCode,
        string expectedCode)
    {
        var (handled, context, response) = await HandleAsync(exception);

        Assert.True(handled);
        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        Assert.Equal(expectedCode, response.Code);
        Assert.False(string.IsNullOrWhiteSpace(response.Description));
    }

    [Fact]
    public async Task TryHandleAsync_HidesUnexpectedExceptionDetails()
    {
        var (_, context, response) = await HandleAsync(
            new InvalidOperationException("Sensitive implementation detail."));

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            context.Response.StatusCode);
        Assert.Equal("Server.UnexpectedError", response.Code);
        Assert.DoesNotContain("Sensitive", response.Description);
    }

    private static async Task<(bool Handled, DefaultHttpContext Context, ApiErrorResponse Response)>
        HandleAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var handler = new ApiExceptionHandler(
            [new DefaultApiExceptionMapper()],
            NullLogger<ApiExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        context.Response.Body.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            context.Response.Body,
            JsonSerializerOptions.Web);

        return (handled, context, Assert.IsType<ApiErrorResponse>(response));
    }
}
