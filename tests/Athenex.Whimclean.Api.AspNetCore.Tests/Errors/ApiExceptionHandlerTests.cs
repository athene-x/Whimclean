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

    [Fact]
    public async Task TryHandleAsync_WithAbortedRequest_WritesNoBody()
    {
        using var abortSource = new CancellationTokenSource();
        await abortSource.CancelAsync();
        var context = new DefaultHttpContext
        {
            RequestAborted = abortSource.Token
        };
        context.Response.Body = new MemoryStream();
        var handler = new ApiExceptionHandler(
            [new DefaultApiExceptionMapper()],
            NullLogger<ApiExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            new OperationCanceledException(),
            CancellationToken.None);

        // The client is gone: handle the exception (no 500 in the logs,
        // nothing rethrown) but do not attempt a response body.
        Assert.True(handled);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task TryHandleAsync_WithServerSideCancellation_StillMaps500()
    {
        // A cancellation NOT caused by the client aborting (e.g. an
        // internal timeout) is a real server error and keeps the 500 body.
        var (handled, context, response) = await HandleAsync(
            new OperationCanceledException());

        Assert.True(handled);
        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            context.Response.StatusCode);
        Assert.Equal("Server.UnexpectedError", response.Code);
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
