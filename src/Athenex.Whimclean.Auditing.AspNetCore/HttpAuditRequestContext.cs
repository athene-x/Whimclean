using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Auditing.AspNetCore;

/// <summary>
/// An <see cref="IAuditRequestContext" /> read from the current HTTP
/// request: the actor name from the <c>name</c>, <c>email</c> or identity
/// name of the principal; the correlation id from
/// <c>X-Correlation-Id</c>, <c>X-Request-Id</c> or the trace identifier;
/// the remote address; and the user agent. Every value is
/// <see langword="null" /> outside a request.
/// </summary>
/// <param name="httpContextAccessor">Supplies the current request.</param>
public sealed class HttpAuditRequestContext(IHttpContextAccessor httpContextAccessor)
    : IAuditRequestContext
{
    /// <summary>Header read first for the correlation id.</summary>
    public const string CorrelationHeader = "X-Correlation-Id";

    /// <summary>Header read second for the correlation id.</summary>
    public const string RequestIdHeader = "X-Request-Id";

    private HttpContext? Http => httpContextAccessor.HttpContext;

    /// <inheritdoc />
    public string? ActorName
    {
        get
        {
            var user = Http?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return FirstValue(user, "name")
                ?? FirstValue(user, "email")
                ?? user.Identity.Name;
        }
    }

    /// <inheritdoc />
    public string? CorrelationId
    {
        get
        {
            if (Http is not { } http)
            {
                return null;
            }

            return FirstHeader(http, CorrelationHeader)
                ?? FirstHeader(http, RequestIdHeader)
                ?? http.TraceIdentifier;
        }
    }

    /// <inheritdoc />
    public string? IpAddress => Http?.Connection.RemoteIpAddress?.ToString();

    /// <inheritdoc />
    public string? UserAgent => Http is { } http ? FirstHeader(http, "User-Agent") : null;

    private static string? FirstValue(ClaimsPrincipal user, string claimType)
    {
        var value = user.FindFirst(claimType)?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? FirstHeader(HttpContext http, string name)
    {
        var values = http.Request.Headers[name];
        var value = values.Count > 0 ? values[0] : null;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
