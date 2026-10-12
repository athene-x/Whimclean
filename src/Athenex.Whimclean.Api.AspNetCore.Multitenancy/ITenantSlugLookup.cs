namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Maps the slug a request names (from a path segment or hostname the
/// web tier forwards in a header) to the tenant identifier. Implemented
/// by the application over its tenant table and used by
/// <see cref="SlugTenantResolutionMiddleware{TTenantId}"/>.
/// </summary>
/// <typeparam name="TTenantId">Type of the tenant identifier.</typeparam>
public interface ITenantSlugLookup<TTenantId>
    where TTenantId : notnull
{
    /// <summary>
    /// Finds the tenant registered under <paramref name="slug"/>, which
    /// arrives trimmed and lower-cased.
    /// </summary>
    /// <returns>
    /// The tenant identifier, or <see langword="null"/> (the default
    /// value for a value type) when no tenant has that slug.
    /// </returns>
    ValueTask<TTenantId?> FindBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}
