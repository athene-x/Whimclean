using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Athenex.Whimclean.Infrastructure.AspNetCoreIdentity.EntityFrameworkCore.Persistence;

/// <summary>
/// Base context for the authentication store (ASP.NET Core Identity's
/// user, role, claim, and token tables), kept separate from the domain
/// DbContext so credential data and business data can evolve and migrate
/// independently.
/// </summary>
/// <typeparam name="TUser">Identity user entity.</typeparam>
/// <typeparam name="TRole">Identity role entity.</typeparam>
/// <typeparam name="TUserId">
/// Key type shared by users and roles.
/// </typeparam>
/// <param name="options">EF Core options for the concrete context.</param>
public abstract class IdentityAuthenticationDbContext<TUser, TRole, TUserId>(
    DbContextOptions options)
    : IdentityDbContext<TUser, TRole, TUserId>(options)
    where TUser : IdentityUser<TUserId>
    where TRole : IdentityRole<TUserId>
    where TUserId : IEquatable<TUserId>;
