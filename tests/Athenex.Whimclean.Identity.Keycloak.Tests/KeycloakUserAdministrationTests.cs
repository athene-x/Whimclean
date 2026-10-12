using System.Net;
using System.Net.Http.Json;
using Athenex.Whimclean.Identity.Keycloak.Administration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Identity.Keycloak.Tests;

public sealed class KeycloakUserAdministrationTests
{
    private static readonly KeycloakAdminOptions Options = new()
    {
        BaseUrl = "http://keycloak:8080",
        Realm = "app",
        ClientId = "api",
        ClientSecret = "secret",
        PasswordSetupClientId = "web",
        PasswordSetupRedirectUrl = "https://app.example.com/"
    };

    private static (KeycloakUserAdministration Client, StubHandler Handler) Create(
        KeycloakAdminOptions? options = null)
    {
        var handler = new StubHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://keycloak:8080/") };
        var tokenProvider = new KeycloakAdminTokenProvider(
            Microsoft.Extensions.Options.Options.Create(options ?? Options),
            new MutableTimeProvider(DateTimeOffset.UtcNow));

        return (new KeycloakUserAdministration(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(options ?? Options),
            tokenProvider,
            NullLogger<KeycloakUserAdministration>.Instance), handler);
    }

    [Fact]
    public async Task CreateUserAsync_SendsTheRepresentationAndReadsTheSubjectFromLocation()
    {
        var (client, handler) = Create();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Headers = { Location = new Uri("http://keycloak:8080/admin/realms/app/users/abc-123") }
        });

        var result = await client.CreateUserAsync("a@example.com", "Alice", null);

        Assert.True(result.IsSuccess);
        Assert.Equal(new KeycloakUser("abc-123", "a@example.com", "Alice", null), result.Value);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/admin/realms/app/users", request.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Contains("\"requiredActions\":[\"UPDATE_PASSWORD\",\"VERIFY_EMAIL\"]", handler.Bodies[0]);
        Assert.Contains("\"username\":\"a@example.com\"", handler.Bodies[0]);
    }

    [Fact]
    public async Task CreateUserAsync_MapsConflictAndFailures()
    {
        var (client, handler) = Create();
        handler
            .Enqueue(_ => new HttpResponseMessage(HttpStatusCode.Conflict))
            .Enqueue(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("down") });

        var conflict = await client.CreateUserAsync("a@example.com", "Alice", null);
        var failure = await client.CreateUserAsync("a@example.com", "Alice", null);

        Assert.Equal(KeycloakErrors.UserAlreadyExists, conflict.Error);
        Assert.Equal(KeycloakErrors.Unavailable, failure.Error);
    }

    [Fact]
    public async Task FindUserByEmailAsync_ReturnsTheMatchOrNotFound()
    {
        var (client, handler) = Create();
        handler
            .Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new { id = "u1", username = "a@example.com", email = "a@example.com", firstName = "Alice", lastName = "Doe" }
                })
            })
            .Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Array.Empty<object>()) });

        var found = await client.FindUserByEmailAsync("a@example.com");
        var missing = await client.FindUserByEmailAsync("b@example.com");

        Assert.Equal(new KeycloakUser("u1", "a@example.com", "Alice", "Doe"), found.Value.User);
        Assert.False(missing.Value.Found);
        Assert.Equal("?email=a%40example.com&exact=true", handler.Requests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task SendPasswordSetupEmailAsync_AppendsTheReturnPathToTheRedirect()
    {
        var (client, handler) = Create();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        var result = await client.SendPasswordSetupEmailAsync("u1", "/invitations/42");

        Assert.True(result.IsSuccess);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("/admin/realms/app/users/u1/execute-actions-email", request.RequestUri!.AbsolutePath);
        Assert.Equal(
            "?client_id=web&redirect_uri=https%3A%2F%2Fapp.example.com%2Finvitations%2F42",
            request.RequestUri.Query);
    }

    [Fact]
    public async Task SetUserEnabledAsync_ReadsThenWritesTheUser()
    {
        var (client, handler) = Create();
        handler
            .Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { id = "u1", enabled = true, firstName = "Alice" })
            })
            .Enqueue(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        var result = await client.SetUserEnabledAsync("u1", enabled: false);

        Assert.True(result.IsSuccess);
        Assert.Equal([HttpMethod.Get, HttpMethod.Put], handler.Requests.Select(r => r.Method));
        Assert.Contains("\"enabled\":false", handler.Bodies[1]);
        Assert.Contains("\"firstName\":\"Alice\"", handler.Bodies[1]);
    }

    [Fact]
    public async Task UpdateUserNameAsync_ReturnsNotFoundForAMissingUser()
    {
        var (client, handler) = Create();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await client.UpdateUserNameAsync("gone", "A", null);

        Assert.Equal(KeycloakErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task DeleteUserAsync_TreatsNotFoundAsSuccess()
    {
        var (client, handler) = Create();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        Assert.True((await client.DeleteUserAsync("gone")).IsSuccess);
    }

    [Fact]
    public async Task TokenProvider_CachesTheTokenUntilItExpires()
    {
        var handler = new StubHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://keycloak:8080/") };
        var clock = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var provider = new KeycloakAdminTokenProvider(Microsoft.Extensions.Options.Options.Create(Options), clock);

        var first = await provider.GetAccessTokenAsync(httpClient);
        var second = await provider.GetAccessTokenAsync(httpClient);
        clock.Now = clock.Now.AddSeconds(280);
        var third = await provider.GetAccessTokenAsync(httpClient);

        Assert.Equal("token-1", first);
        Assert.Equal("token-1", second);
        Assert.Equal("token-2", third);
        Assert.Equal(2, handler.TokenRequests);
    }
}
