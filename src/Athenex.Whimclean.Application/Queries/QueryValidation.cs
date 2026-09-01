namespace Athenex.Whimclean.Application.Queries;

/// <summary>
/// Shared boundary validation for <see cref="ListQueryOptions" /> and
/// <see cref="FacetQueryOptions" />, keeping their normalization rules
/// identical.
/// </summary>
internal static class QueryValidation
{
    internal static string? NormalizeSearchTerm(
        string? searchTerm,
        string parameterName)
    {
        var normalized = string.IsNullOrWhiteSpace(searchTerm)
            ? null
            : searchTerm.Trim();

        if (normalized?.Length > ListQueryOptions.MaximumSearchLength)
        {
            throw new ArgumentException(
                $"The search term cannot exceed " +
                $"{ListQueryOptions.MaximumSearchLength} characters.",
                parameterName);
        }

        return normalized;
    }

    internal static T[] MaterializeTerms<T>(
        IEnumerable<T>? terms,
        int maximum,
        string description,
        string parameterName)
        where T : class
    {
        var materialized = terms?.ToArray() ?? [];

        if (materialized.Length > maximum)
        {
            throw new ArgumentException(
                $"A query cannot contain more than {maximum} {description}.",
                parameterName);
        }

        if (Array.Exists(materialized, term => term is null))
        {
            throw new ArgumentException(
                $"The {description} cannot contain a null element.",
                parameterName);
        }

        return materialized;
    }
}
