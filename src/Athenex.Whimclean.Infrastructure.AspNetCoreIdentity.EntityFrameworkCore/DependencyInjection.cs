using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Infrastructure.AspNetCoreIdentity.EntityFrameworkCore;

/// <summary>
/// Service registrations for ASP.NET Core Identity backed by EF Core
/// stores.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Wires up ASP.NET Core Identity for <typeparamref name="TUser" />
    /// and <typeparamref name="TRole" /> with EF Core stores in
    /// <typeparamref name="TContext" /> and the default token providers.
    /// When <paramref name="configureIdentity" /> is
    /// <see langword="null" /> (the default), the framework's identity
    /// option defaults apply unchanged.
    /// </summary>
    /// <typeparam name="TUser">Identity user entity.</typeparam>
    /// <typeparam name="TRole">Identity role entity.</typeparam>
    /// <typeparam name="TContext">
    /// Context holding the identity store tables.
    /// </typeparam>
    /// <returns>
    /// The <see cref="IdentityBuilder" />, for further configuration.
    /// </returns>
    public static IdentityBuilder AddEntityFrameworkCoreIdentity<
        TUser,
        TRole,
        TContext>(
        this IServiceCollection services,
        Action<IdentityOptions>? configureIdentity = null)
        where TUser : class
        where TRole : class
        where TContext : DbContext
    {
        var builder = configureIdentity is null
            ? services.AddIdentity<TUser, TRole>()
            : services.AddIdentity<TUser, TRole>(configureIdentity);

        return builder
            .AddEntityFrameworkStores<TContext>()
            .AddDefaultTokenProviders();
    }
}
