namespace Athenex.Whimclean.Api.AspNetCore.Operations;

/// <summary>
/// Tags that decide which health checks each endpoint mapped by
/// <see cref="HealthEndpoints.MapHealthEndpoints" /> runs.
/// </summary>
public static class HealthCheckTags
{
    /// <summary>
    /// Marks a check the readiness endpoint runs, such as a database
    /// connectivity check. Checks without it only appear in a full
    /// report.
    /// </summary>
    public const string Ready = "ready";
}
