namespace Athenex.Whimclean.Api.FastEndpoints.Errors;

public sealed record ApiErrorResponse(
    string Code,
    string Description,
    IReadOnlyCollection<ApiErrorDetail>? Errors = null);

public sealed record ApiErrorDetail(
    string Code,
    string Description,
    string? Field = null);
