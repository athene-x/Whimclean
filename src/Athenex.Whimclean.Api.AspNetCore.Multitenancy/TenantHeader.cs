using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.AspNetCore.Multitenancy;

/// <summary>
/// Reads the tenant headers under the rule every resolver shares: a
/// header must appear exactly once, since a repeated one is ambiguous
/// and resolving no tenant is safer than silently picking one of the
/// conflicting values.
/// </summary>
internal static class TenantHeader
{
    public static bool TryGetSingleValue(HttpRequest request, string headerName, out string value)
    {
        var values = request.Headers[headerName];

        if (values.Count == 1 && values[0] is { } single)
        {
            value = single;
            return true;
        }

        value = string.Empty;
        return false;
    }
}
