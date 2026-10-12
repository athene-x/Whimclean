using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;

namespace Athenex.Whimclean.Api.AspNetCore.Operations;

/// <summary>
/// Liveness and readiness endpoints for container orchestrators and load
/// balancers.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>Default path of the liveness endpoint.</summary>
    public const string DefaultLivePath = "/health/live";

    /// <summary>Default path of the readiness endpoint.</summary>
    public const string DefaultReadyPath = "/health/ready";

    /// <summary>
    /// Maps two anonymous endpoints: liveness, which runs no checks and
    /// answers as soon as the process serves requests, and readiness,
    /// which runs the checks tagged <see cref="HealthCheckTags.Ready" />.
    /// Both are exempt from rate limiting. Requires
    /// <c>AddHealthChecks()</c>; register readiness checks with that
    /// tag, for example
    /// <c>AddDbContextCheck&lt;AppDbContext&gt;(tags: [HealthCheckTags.Ready])</c>
    /// from <c>Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore</c>.
    /// </summary>
    /// <param name="endpoints">The route builder to map onto.</param>
    /// <param name="livePath">Liveness path; defaults to <c>/health/live</c>.</param>
    /// <param name="readyPath">Readiness path; defaults to <c>/health/ready</c>.</param>
    /// <returns>The same route builder, for chaining.</returns>
    /// <exception cref="ArgumentException">
    /// A path is <see langword="null" /> or whitespace.
    /// </exception>
    public static IEndpointRouteBuilder MapHealthEndpoints(
        this IEndpointRouteBuilder endpoints,
        string livePath = DefaultLivePath,
        string readyPath = DefaultReadyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(livePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(readyPath);

        endpoints
            .MapHealthChecks(livePath, new HealthCheckOptions { Predicate = _ => false })
            .DisableRateLimiting();
        endpoints
            .MapHealthChecks(
                readyPath,
                new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains(HealthCheckTags.Ready)
                })
            .DisableRateLimiting();

        return endpoints;
    }
}
