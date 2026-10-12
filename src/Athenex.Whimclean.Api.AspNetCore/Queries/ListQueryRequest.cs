using Athenex.Whimclean.Application.Queries;

namespace Athenex.Whimclean.Api.AspNetCore.Queries;

/// <summary>
/// Query-string contract shared by offset-paginated list endpoints:
/// <c>?searchTerm=&amp;sort=&amp;filters=&amp;page=&amp;size=</c>. Derive
/// a request class per endpoint (adding route or extra parameters as
/// needed) and validate it with a
/// <see cref="ListQueryRequestValidator{TRequest}" />. In a Minimal API
/// bind the derived class with <c>[AsParameters]</c>; the FastEndpoints
/// adapter binds it as a regular request DTO. See
/// <see cref="ListQueryParser" /> for the <see cref="Sort" /> and
/// <see cref="Filters" /> syntax.
/// </summary>
public abstract class ListQueryRequest
{
    /// <summary>Free text matched by the endpoint's search fields.</summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Comma-separated sort fields, <c>-</c> prefix for descending.
    /// </summary>
    public string? Sort { get; init; }

    /// <summary>
    /// Filters in the form <c>field:operator:value</c>, one per repeated
    /// query parameter. Minimal API binding keeps each repeated value
    /// whole, so a comma-separated <c>in</c> value stays in one filter;
    /// the FastEndpoints adapter registers a binding that does the same.
    /// </summary>
    public string[]? Filters { get; init; } = [];

    /// <summary>One-based page number.</summary>
    public int? Page { get; init; } = ListQueryOptions.DefaultPage;

    /// <summary>Items per page.</summary>
    public int? Size { get; init; } = ListQueryOptions.DefaultSize;

    /// <summary>
    /// Parses the raw values into a validated
    /// <see cref="ListQueryOptions" />. Run the
    /// <see cref="ListQueryRequestValidator{TRequest}" /> first so
    /// malformed input surfaces as a 400 rather than an exception.
    /// </summary>
    public ListQueryOptions ToListQueryOptions() =>
        ListQueryParser.Parse(
            SearchTerm,
            Sort,
            Filters ?? [],
            Page ?? ListQueryOptions.DefaultPage,
            Size ?? ListQueryOptions.DefaultSize);
}
