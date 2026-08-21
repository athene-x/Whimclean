using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Infrastructure.AspNetCoreIdentity.EntityFrameworkCore;

public static class DependencyInjection
{
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
