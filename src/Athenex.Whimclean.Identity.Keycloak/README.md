# Athenex.Whimclean.Identity.Keycloak

[ภาษาไทย](README-TH.md)

Keycloak integration for Whimclean APIs: JWT bearer authentication with the claim conventions Keycloak tokens use, and an admin API client for provisioning users with a service account.

## Install

```shell
dotnet add package Athenex.Whimclean.Identity.Keycloak
```

## Authenticate API calls

```csharp
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Identity.Keycloak;

builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddClaimsCurrentUser<Guid>(options => options.ClaimType = "sub");
```

```json
{
  "Keycloak": {
    "Authority": "https://auth.example.com/realms/app",
    "Audience": "app-api",
    "MetadataAddress": "http://keycloak:8080/realms/app/.well-known/openid-configuration",
    "RequireHttpsMetadata": true
  }
}
```

`AddKeycloakAuthentication(configuration)` binds `KeycloakOptions` from the `Keycloak` section (a different section name can be passed) and throws `InvalidOperationException` when the section is missing or invalid; the other overload takes a `KeycloakOptions` instance. `Authority` must be an absolute URL and `Audience` is required. `MetadataAddress` is optional and defaults to `{Authority}/.well-known/openid-configuration`; `RequireHttpsMetadata` defaults to `true`.

The registration turns inbound claim mapping off so `sub` stays `sub`, pins the valid issuer to `Authority` even when discovery runs against an internal `MetadataAddress`, validates the audience, takes the name from `preferred_username` and roles from `roles` (both configurable through `NameClaimType` and `RoleClaimType`), and registers authorization services. When the local user id lives in your own table, use `AddLocalUserResolution<Guid>` from `Athenex.Whimclean.Api.AspNetCore` instead of the claims current user.

## Provision users

```csharp
builder.Services.AddKeycloakAdministration(builder.Configuration);
```

```json
{
  "KeycloakAdmin": {
    "BaseUrl": "http://keycloak:8080",
    "Realm": "app",
    "ClientId": "app-api",
    "ClientSecret": "<set-via-user-secrets>",
    "PasswordSetupClientId": "app-web",
    "PasswordSetupRedirectUrl": "https://app.example.com/"
  }
}
```

`AddKeycloakAdministration(configuration)` binds `KeycloakAdminOptions` from the `KeycloakAdmin` section (a different section name can be passed); the other overload takes an `Action<KeycloakAdminOptions>`. The client needs the `manage-users` and `view-users` service-account roles. Options are validated at startup: `BaseUrl` must be an absolute URL, `Realm`, `ClientId` and `ClientSecret` are required, and when `PasswordSetupRedirectUrl` is set it must be an absolute URL and `PasswordSetupClientId` is required. `IKeycloakUserAdministration` then offers:

| Method | What it does |
| --- | --- |
| `FindUserByEmailAsync` | exact email match, `Result<KeycloakUserLookup>` with `KeycloakUserLookup.NotFound` when none |
| `CreateUserAsync` | enabled user named by email with the `VERIFY_EMAIL` and `UPDATE_PASSWORD` required actions; returns the `KeycloakUser`, or `UserAlreadyExists` on a conflict |
| `RequiresPasswordSetupAsync` | whether `UPDATE_PASSWORD` is still pending |
| `SendPasswordSetupEmailAsync` | the actions email, redirecting to `PasswordSetupRedirectUrl` plus an optional path when that URL is configured |
| `UpdateUserNameAsync`, `SetUserEnabledAsync` | read-modify-write of the user representation |
| `DeleteUserAsync` | succeeds when the user is already gone |

`KeycloakUser` carries `Subject` (the `sub` of the user's tokens), `Email`, `FirstName` and `LastName`; a user Keycloak has no names for is given the user name as `FirstName`.

Every call returns a `Result` with one of the `KeycloakErrors` instead of throwing: `Unavailable` (the API failed or could not be reached), `UserAlreadyExists`, `UserNotFound` (the user no longer exists) and `PasswordSetupEmailNotSent`. Keycloak's response bodies are logged, never returned to callers. The service-account token is obtained with the client credentials grant, cached by the singleton `KeycloakAdminTokenProvider` and refreshed thirty seconds before expiry.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
