using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Athenex.Whimclean.Infrastructure.AspNetCoreIdentity.EntityFrameworkCore.Persistence;

public abstract class IdentityAuthenticationDbContext<TUser, TRole, TUserId>(
    DbContextOptions options)
    : IdentityDbContext<TUser, TRole, TUserId>(options)
    where TUser : IdentityUser<TUserId>
    where TRole : IdentityRole<TUserId>
    where TUserId : IEquatable<TUserId>;
