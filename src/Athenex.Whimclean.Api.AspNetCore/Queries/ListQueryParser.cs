using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Athenex.Whimclean.Application.Queries;

namespace Athenex.Whimclean.Api.AspNetCore.Queries;

/// <summary>
/// Parses the raw query-string values of a list request into a
/// <see cref="ListQueryOptions" />.
/// <para>
/// Filters use the pattern <c>field:operator:value</c>, one per query
/// parameter (<c>?filters=status:eq:active&amp;filters=name:cn:order</c>).
/// Supported operators: <c>eq</c>, <c>ne</c>, <c>gt</c>, <c>ge</c>,
/// <c>lt</c>, <c>le</c>, <c>cn</c> (contains), <c>sw</c> (starts with),
/// <c>ew</c> (ends with) and <c>in</c>, whose value is a comma-separated
/// list (<c>status:in:draft,published</c>).
/// </para>
/// <para>
/// Sort is a comma-separated list of field names; prefix a field with
/// <c>-</c> to sort it descending (<c>?sort=name,-createdAt</c>).
/// </para>
/// </summary>
public static class ListQueryParser
{
    private const char PartSeparator = ':';
    private const char ListSeparator = ',';
    private const char DescendingPrefix = '-';

    private static readonly FrozenDictionary<string, FilterOperator> Operators =
        new Dictionary<string, FilterOperator>
        {
            ["eq"] = FilterOperator.Equal,
            ["ne"] = FilterOperator.NotEqual,
            ["gt"] = FilterOperator.GreaterThan,
            ["ge"] = FilterOperator.GreaterThanOrEqual,
            ["lt"] = FilterOperator.LessThan,
            ["le"] = FilterOperator.LessThanOrEqual,
            ["cn"] = FilterOperator.Contains,
            ["sw"] = FilterOperator.StartsWith,
            ["ew"] = FilterOperator.EndsWith,
            ["in"] = FilterOperator.In
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly string SupportedOperators = string.Join(", ", Operators.Keys);

    /// <summary>
    /// Parses every raw value into a validated
    /// <see cref="ListQueryOptions" />.
    /// </summary>
    /// <exception cref="FormatException">
    /// A sort or filter value is malformed.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The parsed values violate a <see cref="ListQueryOptions" /> limit.
    /// </exception>
    public static ListQueryOptions Parse(
        string? searchTerm,
        string? sort,
        IEnumerable<string>? filters,
        int page = ListQueryOptions.DefaultPage,
        int size = ListQueryOptions.DefaultSize) =>
        new(searchTerm, ParseSort(sort), ParseFilters(filters), page, size);

    /// <summary>
    /// Parses each raw filter with <see cref="ParseFilter" />; a
    /// <see langword="null" /> sequence yields an empty list.
    /// </summary>
    /// <exception cref="FormatException">
    /// A filter is malformed, or more than
    /// <see cref="ListQueryOptions.MaximumFilters" /> were supplied.
    /// </exception>
    public static IReadOnlyList<FilterTerm> ParseFilters(IEnumerable<string>? filters)
    {
        if (filters is null)
        {
            return [];
        }

        var terms = filters.Select(ParseFilter).ToList();

        if (terms.Count > ListQueryOptions.MaximumFilters)
        {
            throw new FormatException(
                $"At most {ListQueryOptions.MaximumFilters} filters can be supplied.");
        }

        return terms;
    }

    /// <summary>
    /// Parses a single <c>field:operator:value</c> filter into a
    /// <see cref="FilterTerm" />.
    /// </summary>
    /// <exception cref="FormatException">The filter is malformed.</exception>
    public static FilterTerm ParseFilter(string? filter)
    {
        if (!TryParseFilter(filter, out var term, out var error))
        {
            throw new FormatException(error);
        }

        return term;
    }

    /// <summary>
    /// Attempts to parse a single <c>field:operator:value</c> filter,
    /// returning a human-readable <paramref name="error" /> on failure.
    /// </summary>
    public static bool TryParseFilter(
        string? filter,
        [NotNullWhen(true)] out FilterTerm? term,
        [NotNullWhen(false)] out string? error)
    {
        term = null;

        if (string.IsNullOrWhiteSpace(filter))
        {
            error = "A filter cannot be empty.";
            return false;
        }

        var parts = filter.Split(PartSeparator, 3);

        if (parts.Length != 3)
        {
            error = $"Filter '{filter}' must use the pattern 'field:operator:value'.";
            return false;
        }

        var field = parts[0].Trim();
        var operatorCode = parts[1].Trim();
        var rawValue = parts[2];

        if (field.Length == 0)
        {
            error = $"Filter '{filter}' has an empty field name.";
            return false;
        }

        if (!Operators.TryGetValue(operatorCode, out var @operator))
        {
            error = $"Filter '{filter}' uses unknown operator '{operatorCode}'. " +
                    $"Supported operators: {SupportedOperators}.";
            return false;
        }

        if (@operator == FilterOperator.In)
        {
            var values = rawValue.Split(ListSeparator);

            if (Array.Exists(values, value => value.Length == 0))
            {
                error = $"Filter '{filter}' must list at least one non-empty value, separated by commas.";
                return false;
            }

            if (values.Length > FilterTerm.MaximumValues)
            {
                error = $"Filter '{filter}' lists more than {FilterTerm.MaximumValues} values.";
                return false;
            }

            term = new FilterTerm(field, values);
        }
        else
        {
            if (rawValue.Length == 0)
            {
                error = $"Filter '{filter}' has an empty value.";
                return false;
            }

            term = new FilterTerm(field, rawValue, @operator);
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Parses a comma-separated sort expression (<c>-</c> prefix for
    /// descending) into ordered <see cref="SortTerm" />s; blank input
    /// yields an empty list.
    /// </summary>
    /// <exception cref="FormatException">The expression is malformed.</exception>
    public static IReadOnlyList<SortTerm> ParseSort(string? sort)
    {
        if (!TryParseSort(sort, out var terms, out var error))
        {
            throw new FormatException(error);
        }

        return terms;
    }

    /// <summary>
    /// Attempts to parse a comma-separated sort expression, returning a
    /// human-readable <paramref name="error" /> on failure.
    /// </summary>
    public static bool TryParseSort(
        string? sort,
        out IReadOnlyList<SortTerm> terms,
        [NotNullWhen(false)] out string? error)
    {
        terms = [];

        if (string.IsNullOrWhiteSpace(sort))
        {
            error = null;
            return true;
        }

        var segments = sort.Split(ListSeparator);

        if (segments.Length > ListQueryOptions.MaximumSortTerms)
        {
            error = $"At most {ListQueryOptions.MaximumSortTerms} sort fields can be supplied.";
            return false;
        }

        var parsed = new List<SortTerm>(segments.Length);

        foreach (var segment in segments)
        {
            if (!TryParseSortTerm(segment, out var term, out error))
            {
                return false;
            }

            parsed.Add(term);
        }

        terms = parsed;
        error = null;
        return true;
    }

    private static bool TryParseSortTerm(
        string segment,
        [NotNullWhen(true)] out SortTerm? term,
        [NotNullWhen(false)] out string? error)
    {
        term = null;

        var field = segment.Trim();
        var direction = SortDirection.Ascending;

        if (field.StartsWith(DescendingPrefix))
        {
            direction = SortDirection.Descending;
            field = field[1..].Trim();
        }

        if (field.Length == 0)
        {
            error = "Sort contains an empty field name.";
            return false;
        }

        term = new SortTerm(field, direction);
        error = null;
        return true;
    }
}
