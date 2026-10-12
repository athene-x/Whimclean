using System.Security.Cryptography;
using System.Text;

namespace Athenex.Whimclean.Infrastructure.Seeding;

/// <summary>
/// Derives stable identifiers for seeded rows (system roles, catalog
/// entries) from a parent identifier and a key, so re-running a seeder or
/// a migration produces the same ids on every environment.
/// </summary>
public static class DeterministicGuid
{
    /// <summary>
    /// Hashes <c>{parentId}:{key}</c> with SHA-256 and takes the first
    /// sixteen bytes as the identifier.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is <see langword="null" /> or whitespace.
    /// </exception>
    public static Guid Create(Guid parentId, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{parentId:N}:{key}"));

        return new Guid(hash.AsSpan(0, 16));
    }

    /// <summary>
    /// Hashes <paramref name="key" /> alone, for top-level seeded rows.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is <see langword="null" /> or whitespace.
    /// </exception>
    public static Guid Create(string key) => Create(Guid.Empty, key);
}
