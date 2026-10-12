using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Identity.Keycloak.Administration;

/// <summary>
/// Obtains and caches the service-account access token the admin client
/// sends, refreshing it shortly before it expires. One instance serves
/// every request, so register it as a singleton.
/// </summary>
/// <param name="options">The service account and realm.</param>
/// <param name="timeProvider">Clock used to judge expiry.</param>
public sealed class KeycloakAdminTokenProvider(
    IOptions<KeycloakAdminOptions> options,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan ExpirySkew = TimeSpan.FromSeconds(30);

    private readonly KeycloakAdminOptions _options = options.Value;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private CachedToken? _cached;

    /// <summary>
    /// Returns the cached token, or requests a new one with the client
    /// credentials grant through <paramref name="httpClient" />.
    /// </summary>
    /// <exception cref="HttpRequestException">
    /// The token endpoint failed or returned no access token.
    /// </exception>
    public async Task<string> GetAccessTokenAsync(
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        // The common path reads the cached token without taking the lock;
        // the lock only serializes refreshes.
        if (TryGetValid(out var accessToken))
        {
            return accessToken;
        }

        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (TryGetValid(out accessToken))
            {
                return accessToken;
            }

            var token = await RequestTokenAsync(httpClient, cancellationToken);
            var expiresAt = timeProvider.GetUtcNow()
                .AddSeconds(token.ExpiresIn)
                .Subtract(ExpirySkew);

            _cached = new CachedToken(token.AccessToken, expiresAt);

            return token.AccessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool TryGetValid([NotNullWhen(true)] out string? accessToken)
    {
        var cached = _cached;

        if (cached is not null && timeProvider.GetUtcNow() < cached.ExpiresAt)
        {
            accessToken = cached.AccessToken;
            return true;
        }

        accessToken = null;
        return false;
    }

    private async Task<(string AccessToken, int ExpiresIn)> RequestTokenAsync(
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(
            $"realms/{_options.Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret
            }),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Keycloak token request failed with status {(int)response.StatusCode}.",
                inner: null,
                response.StatusCode);
        }

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);

        if (string.IsNullOrWhiteSpace(token?.AccessToken))
        {
            throw new HttpRequestException("Keycloak token response did not contain an access token.");
        }

        return (token.AccessToken, token.ExpiresIn);
    }

    private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresAt);

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }
}
