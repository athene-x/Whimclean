namespace Athenex.Whimclean.Api.AspNetCore.Errors;

/// <summary>
/// The single error body returned by every failure response of the API,
/// regardless of which layer produced the failure. Clients can rely on
/// this shape for all non-success status codes.
/// </summary>
/// <param name="Code">
/// Stable, machine-readable error code (e.g. <c>"Server.UnexpectedError"</c>)
/// intended for programmatic handling.
/// </param>
/// <param name="Description">
/// Human-readable explanation of the failure, safe to show to clients.
/// </param>
/// <param name="Errors">
/// Per-field details when the failure is a validation error; otherwise
/// <see langword="null"/> (the default).
/// </param>
public sealed record ApiErrorResponse(
    string Code,
    string Description,
    IReadOnlyCollection<ApiErrorDetail>? Errors = null);

/// <summary>
/// A single per-field entry inside <see cref="ApiErrorResponse.Errors"/>,
/// typically describing one validation failure.
/// </summary>
/// <param name="Code">
/// Stable, machine-readable code identifying this specific failure.
/// </param>
/// <param name="Description">
/// Human-readable explanation of this specific failure.
/// </param>
/// <param name="Field">
/// Name of the offending request field in camelCase, or
/// <see langword="null"/> (the default) when the detail is not tied to a
/// particular field.
/// </param>
public sealed record ApiErrorDetail(
    string Code,
    string Description,
    string? Field = null);
