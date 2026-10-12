using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace Athenex.Whimclean.Api.AspNetCore.Errors;

/// <summary>
/// Maps FluentValidation failures to the shared
/// <see cref="ApiErrorResponse"/> body, so Minimal API validation, the
/// FastEndpoints adapter, and hand-written validation all answer with the
/// same 400 shape.
/// </summary>
public static class ValidationHttpMapper
{
    /// <summary>
    /// Error code carried by every validation failure response.
    /// </summary>
    public const string ValidationFailedCode = "Validation.Failed";

    /// <summary>
    /// Builds the <c>"Validation.Failed"</c> response: one
    /// <see cref="ApiErrorDetail"/> per failure, carrying the failure's
    /// error code, message, and the offending property name in camelCase
    /// (or <see langword="null"/> when the failure is not tied to a
    /// property).
    /// </summary>
    /// <param name="failures">The validation failures to render.</param>
    /// <returns>The error body describing the failures.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="failures"/> is <see langword="null"/>.
    /// </exception>
    public static ApiErrorResponse ToApiErrorResponse(
        this IEnumerable<ValidationFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);

        return new ApiErrorResponse(
            ValidationFailedCode,
            "One or more validation errors occurred.",
            failures
                .Select(failure => new ApiErrorDetail(
                    failure.ErrorCode,
                    failure.ErrorMessage,
                    string.IsNullOrEmpty(failure.PropertyName)
                        ? null
                        : JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName)))
                .ToArray());
    }

    /// <summary>
    /// Produces the 400 JSON response for a failed validation.
    /// </summary>
    /// <param name="result">The validation outcome to translate.</param>
    /// <returns>
    /// A 400 <see cref="IResult"/> whose body is the
    /// <see cref="ApiErrorResponse"/> built by
    /// <see cref="ToApiErrorResponse"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="result"/> is valid; a success cannot be mapped to
    /// an error response.
    /// </exception>
    public static IResult ToHttpResult(this ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsValid)
        {
            throw new InvalidOperationException(
                "A valid result cannot be mapped to an error response.");
        }

        return Results.Json(
            result.Errors.ToApiErrorResponse(),
            statusCode: StatusCodes.Status400BadRequest);
    }
}
