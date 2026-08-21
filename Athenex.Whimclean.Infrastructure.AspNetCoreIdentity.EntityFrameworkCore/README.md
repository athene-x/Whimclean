# Athenex.Whimclean.Infrastructure.AspNetCoreIdentity.EntityFrameworkCore

Optional ASP.NET Core Identity storage and dependency injection for EF Core applications.

## Install

```shell
dotnet add package Athenex.Whimclean.Infrastructure.AspNetCoreIdentity.EntityFrameworkCore
```

Install a database provider separately, such as `Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL`.

## Define the infrastructure user

The Identity user is an authentication/storage model and does not need to be a domain entity:

```csharp
using Microsoft.AspNetCore.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}
```

## Create the Identity context

```csharp
using Athenex.Whimclean.Infrastructure.AspNetCoreIdentity.EntityFrameworkCore.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public sealed class AuthenticationDbContext(
    DbContextOptions<AuthenticationDbContext> options)
    : IdentityAuthenticationDbContext<
        ApplicationUser,
        IdentityRole<Guid>,
        Guid>(options);
```

## Register Identity with PostgreSQL

```csharp
using Athenex.Whimclean.Infrastructure.AspNetCoreIdentity.EntityFrameworkCore;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL;

services.AddPostgreSqlContext<AuthenticationDbContext>(
    identityConnectionString);

services.AddEntityFrameworkCoreIdentity<
    ApplicationUser,
    IdentityRole<Guid>,
    AuthenticationDbContext>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
    });
```

Registration adds the EF stores, default token providers, `UserManager<ApplicationUser>`, and `SignInManager<ApplicationUser>`.

With another provider, register `AuthenticationDbContext` through `AddEntityFrameworkCoreContext` and call `UseSqlServer`, `UseSqlite`, or the chosen provider configuration.

## Clean-architecture boundary

- Use `UserManager` in infrastructure account adapters.
- Use `SignInManager` at the ASP.NET Core/API boundary.
- Keep application and domain code dependent on focused account contracts and `ICurrentUser<TUserId>`, not Identity types.
- Keep tenant membership in a consumer-owned model. An Identity user may belong to zero, one, or many tenants.

This package stores users and configures sign-in services. It does not issue OAuth/OIDC access tokens and does not include OpenIddict. Add an authorization server separately only when the application requires standards-based SSO, multiple clients, or multiple APIs.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
