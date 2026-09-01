using System.Diagnostics.CodeAnalysis;

namespace Athenex.Whimclean.Multitenancy;

/// <summary>
/// An <see cref="ICurrentTenant{TTenantId}" /> backed by an ambient
/// async-local scope. Register it in hosts that have no HTTP tenant
/// resolution — background workers, queue consumers, seeders — and wrap
/// tenant-scoped work in <see cref="Begin" />:
/// <code>
/// using (AmbientCurrentTenant&lt;Guid&gt;.Begin(tenantId))
/// {
///     // writes here are attributed and filtered to tenantId
/// }
/// </code>
/// Scopes nest; disposing restores the previous tenant.
/// </summary>
public sealed class AmbientCurrentTenant<TTenantId> : ICurrentTenant<TTenantId>
    where TTenantId : notnull
{
    private static readonly AsyncLocal<TenantHolder?> Current = new();

    /// <summary>
    /// Retrieves the tenant of the innermost <see cref="Begin" /> scope
    /// enclosing the current async flow. Returns
    /// <see langword="false" /> — leaving <paramref name="tenantId" />
    /// default — when no scope is active.
    /// </summary>
    /// <param name="tenantId">
    /// The scoped tenant id; meaningful only when the method returns
    /// <see langword="true" />.
    /// </param>
    public bool TryGetTenantId([MaybeNullWhen(false)] out TTenantId tenantId)
    {
        var holder = Current.Value;

        if (holder is null)
        {
            tenantId = default;
            return false;
        }

        tenantId = holder.TenantId;
        return true;
    }

    /// <summary>
    /// Opens an ambient tenant scope: until the returned token is
    /// disposed, <see cref="TryGetTenantId" /> yields
    /// <paramref name="tenantId" /> within the current async flow.
    /// Scopes nest; disposing restores whatever tenant (or none) was in
    /// effect before. The caller vouches for the tenant — no access
    /// validation happens here.
    /// </summary>
    /// <param name="tenantId">The tenant to establish; must not be null.</param>
    /// <returns>A token that ends the scope when disposed.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="tenantId" /> is <see langword="null" />.
    /// </exception>
    public static IDisposable Begin(TTenantId tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        var previous = Current.Value;
        Current.Value = new TenantHolder(tenantId);

        return new TenantScope(previous);
    }

    private sealed record TenantHolder(TTenantId TenantId);

    private sealed class TenantScope(TenantHolder? previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }
}
