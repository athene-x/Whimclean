using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Athenex.Whimclean.Api.AspNetCore.Operations;

/// <summary>
/// A global fixed-window rate limit partitioned per caller: by the
/// authenticated subject when there is one, otherwise by remote address.
/// </summary>
public static class RequestRateLimiting
{
    /// <summary>
    /// Claim type the partition key is taken from for authenticated
    /// callers.
    /// </summary>
    public const string SubjectClaimType = "sub";

    /// <summary>
    /// Registers the limiter from the <c>RateLimiting</c> section of
    /// <paramref name="configuration" /> (defaults apply when the section
    /// is absent). Pair with <c>app.UseRateLimiter()</c> after
    /// authentication so the partition sees the subject.
    /// </summary>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// The permit limit or window is not positive.
    /// </exception>
    public static IServiceCollection AddRequestRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = RequestRateLimitOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(sectionName).Get<RequestRateLimitOptions>()
            ?? new RequestRateLimitOptions();

        return services.AddRequestRateLimiting(options);
    }

    /// <summary>
    /// Registers the limiter with the given options. Pair with
    /// <c>app.UseRateLimiter()</c> after authentication so the partition
    /// sees the subject.
    /// </summary>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// The permit limit or window is not positive.
    /// </exception>
    public static IServiceCollection AddRequestRateLimiting(
        this IServiceCollection services,
        RequestRateLimitOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Validate() is { } failure)
        {
            throw new InvalidOperationException(failure);
        }

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.WindowSeconds)
                    }));
        });

        return services;
    }

    /// <summary>
    /// The key a request is limited under: <c>user:{sub}</c> for an
    /// authenticated caller, otherwise <c>ip:{address}</c>.
    /// </summary>
    public static string PartitionKey(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var subject = httpContext.User.FindFirst(SubjectClaimType)?.Value;

        return string.IsNullOrWhiteSpace(subject)
            ? $"ip:{httpContext.Connection.RemoteIpAddress}"
            : $"user:{subject}";
    }
}
