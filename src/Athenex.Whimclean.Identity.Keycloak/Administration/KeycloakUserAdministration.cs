using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Athenex.Whimclean.Domain.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Athenex.Whimclean.Identity.Keycloak.Administration;

/// <summary>
/// <see cref="IKeycloakUserAdministration" /> over the Keycloak admin
/// REST API. Failures are logged with the response body and returned as
/// <see cref="KeycloakErrors" />; nothing from Keycloak reaches the
/// caller's response.
/// </summary>
/// <param name="httpClient">Client whose base address is the Keycloak URL.</param>
/// <param name="options">Realm and password-setup settings.</param>
/// <param name="tokenProvider">Supplies the service-account token.</param>
/// <param name="logger">Receives failed requests.</param>
public sealed class KeycloakUserAdministration(
    HttpClient httpClient,
    IOptions<KeycloakAdminOptions> options,
    KeycloakAdminTokenProvider tokenProvider,
    ILogger<KeycloakUserAdministration> logger)
    : IKeycloakUserAdministration
{
    private const string UpdatePasswordAction = "UPDATE_PASSWORD";

    private static readonly string[] PasswordSetupActions = [UpdatePasswordAction, "VERIFY_EMAIL"];

    private readonly KeycloakAdminOptions _options = options.Value;

    private string UsersUrl => $"admin/realms/{_options.Realm}/users";

    /// <inheritdoc />
    public async ValueTask<Result<KeycloakUserLookup>> FindUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        try
        {
            using var response = await SendAsync(
                HttpMethod.Get,
                $"{UsersUrl}?email={Uri.EscapeDataString(email)}&exact=true",
                content: null,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await FailAsync(response, cancellationToken);
            }

            var users = await response.Content
                .ReadFromJsonAsync<List<KeycloakUserRepresentation>>(cancellationToken);
            var user = users?.FirstOrDefault();

            return user?.Id is null
                ? KeycloakUserLookup.NotFound
                : new KeycloakUserLookup(ToUser(user, email));
        }
        catch (HttpRequestException exception)
        {
            return Fail(exception);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Result<KeycloakUser>> CreateUserAsync(
        string email,
        string firstName,
        string? lastName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);

        try
        {
            var representation = new KeycloakUserRepresentation
            {
                Username = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Enabled = true,
                EmailVerified = false,
                RequiredActions = PasswordSetupActions
            };

            using var response = await SendAsync(
                HttpMethod.Post,
                UsersUrl,
                JsonContent.Create(representation),
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return KeycloakErrors.UserAlreadyExists;
            }

            if (!response.IsSuccessStatusCode)
            {
                return await FailAsync(response, cancellationToken);
            }

            var subject = response.Headers.Location?.Segments.LastOrDefault()?.Trim('/');

            if (string.IsNullOrWhiteSpace(subject))
            {
                logger.LogError("Keycloak created user {Email} without a Location header.", email);
                return KeycloakErrors.Unavailable;
            }

            return new KeycloakUser(subject, email, firstName, lastName);
        }
        catch (HttpRequestException exception)
        {
            return Fail(exception);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Result<bool>> RequiresPasswordSetupAsync(
        string subject,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        try
        {
            using var response = await SendAsync(
                HttpMethod.Get,
                UserUrl(subject),
                content: null,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return KeycloakErrors.UserNotFound;
            }

            if (!response.IsSuccessStatusCode)
            {
                return await FailAsync(response, cancellationToken);
            }

            var user = await response.Content
                .ReadFromJsonAsync<KeycloakUserRepresentation>(cancellationToken);

            return user?.RequiredActions?.Contains(UpdatePasswordAction) == true;
        }
        catch (HttpRequestException exception)
        {
            return Fail(exception);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Result> SendPasswordSetupEmailAsync(
        string subject,
        string? returnPath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        try
        {
            using var response = await SendAsync(
                HttpMethod.Put,
                $"{UserUrl(subject)}/execute-actions-email{PasswordSetupQuery(returnPath)}",
                JsonContent.Create(PasswordSetupActions),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            return await FailAsync(response, cancellationToken, KeycloakErrors.PasswordSetupEmailNotSent);
        }
        catch (HttpRequestException exception)
        {
            return Fail(exception);
        }
    }

    /// <inheritdoc />
    public ValueTask<Result> UpdateUserNameAsync(
        string subject,
        string firstName,
        string? lastName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);

        return UpdateUserAsync(
            subject,
            user =>
            {
                user["firstName"] = firstName;
                user["lastName"] = lastName ?? string.Empty;
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Result> SetUserEnabledAsync(
        string subject,
        bool enabled,
        CancellationToken cancellationToken = default) =>
        UpdateUserAsync(subject, user => user["enabled"] = enabled, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<Result> DeleteUserAsync(
        string subject,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        try
        {
            using var response = await SendAsync(
                HttpMethod.Delete,
                UserUrl(subject),
                content: null,
                cancellationToken);

            return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound
                ? Result.Success()
                : await FailAsync(response, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            return Fail(exception);
        }
    }

    private async ValueTask<Result> UpdateUserAsync(
        string subject,
        Action<JsonObject> change,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        try
        {
            using var current = await SendAsync(
                HttpMethod.Get,
                UserUrl(subject),
                content: null,
                cancellationToken);

            if (current.StatusCode == HttpStatusCode.NotFound)
            {
                return KeycloakErrors.UserNotFound;
            }

            if (!current.IsSuccessStatusCode)
            {
                return await FailAsync(current, cancellationToken);
            }

            var user = await current.Content.ReadFromJsonAsync<JsonObject>(cancellationToken);

            if (user is null)
            {
                return KeycloakErrors.Unavailable;
            }

            change(user);

            using var response = await SendAsync(
                HttpMethod.Put,
                UserUrl(subject),
                JsonContent.Create(user),
                cancellationToken);

            return response.IsSuccessStatusCode
                ? Result.Success()
                : await FailAsync(response, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            return Fail(exception);
        }
    }

    private string UserUrl(string subject) => $"{UsersUrl}/{Uri.EscapeDataString(subject)}";

    private string PasswordSetupQuery(string? returnPath)
    {
        if (string.IsNullOrWhiteSpace(_options.PasswordSetupRedirectUrl))
        {
            return string.Empty;
        }

        var redirectUrl = returnPath is null
            ? _options.PasswordSetupRedirectUrl
            : $"{_options.PasswordSetupRedirectUrl.TrimEnd('/')}{returnPath}";

        return $"?client_id={Uri.EscapeDataString(_options.PasswordSetupClientId!)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUrl)}";
    }

    private static KeycloakUser ToUser(KeycloakUserRepresentation user, string fallbackEmail)
    {
        var email = string.IsNullOrWhiteSpace(user.Email) ? fallbackEmail : user.Email;
        var firstName = string.IsNullOrWhiteSpace(user.FirstName) ? null : user.FirstName;
        var lastName = string.IsNullOrWhiteSpace(user.LastName) ? null : user.LastName;

        // The first non-blank name becomes the first name, so a user with
        // only a last name is still named.
        return (firstName, lastName) switch
        {
            (null, null) => new KeycloakUser(
                user.Id!,
                email,
                string.IsNullOrWhiteSpace(user.Username) ? email : user.Username,
                null),
            (null, _) => new KeycloakUser(user.Id!, email, lastName, null),
            _ => new KeycloakUser(user.Id!, email, firstName, lastName)
        };
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativeUrl,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, relativeUrl) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await tokenProvider.GetAccessTokenAsync(httpClient, cancellationToken));

        return await httpClient.SendAsync(request, cancellationToken);
    }

    private async Task<Error> FailAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken,
        Error? error = null)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        logger.LogError(
            "Keycloak admin request {Method} {Url} failed with status {StatusCode}: {Body}",
            response.RequestMessage?.Method,
            response.RequestMessage?.RequestUri,
            (int)response.StatusCode,
            body);

        return error ?? KeycloakErrors.Unavailable;
    }

    private Error Fail(HttpRequestException exception)
    {
        logger.LogError(exception, "Keycloak admin request failed.");

        return KeycloakErrors.Unavailable;
    }
}
