using Athenex.Whimclean.Application.Queries;
using FastEndpoints;
using FluentValidation;

namespace Athenex.Whimclean.Api.FastEndpoints.Queries;

/// <summary>
/// Validates the shared list query parameters so that
/// <see cref="ListQueryRequest.ToListQueryOptions" /> cannot throw, and
/// lets each endpoint declare the sort and filter fields it supports.
/// Derive a concrete validator per request type; FastEndpoints only
/// registers concrete validators.
/// </summary>
/// <typeparam name="TRequest">Request type being validated.</typeparam>
public abstract class ListQueryRequestValidator<TRequest> : Validator<TRequest>
    where TRequest : ListQueryRequest
{
    private static readonly FilterOperator[] DefaultFilterOperators =
        [FilterOperator.Equal, FilterOperator.In];

    private readonly Dictionary<string, FilterFieldRule> _filterFields =
        new(StringComparer.Ordinal);

    private HashSet<string>? _sortFields;

    /// <summary>
    /// Registers the rules every list request must satisfy: search term
    /// length, page and size ranges, and well-formed sort and filter
    /// values. Field allow-lists are added by the derived validator
    /// through <see cref="AllowSortFields" /> and
    /// <see cref="AllowFilterField(string, FilterOperator[])" />.
    /// </summary>
    protected ListQueryRequestValidator()
    {
        RuleFor(request => request.SearchTerm)
            .MaximumLength(ListQueryOptions.MaximumSearchLength);

        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(request => request.Size)
            .InclusiveBetween(1, ListQueryOptions.MaximumSize);

        RuleFor(request => request.Sort)
            .Custom((sort, context) =>
            {
                if (!ListQueryParser.TryParseSort(sort, out var terms, out var error))
                {
                    context.AddFailure(error);
                    return;
                }

                if (_sortFields is null)
                {
                    return;
                }

                foreach (var term in terms.Where(term => !_sortFields.Contains(term.Field)))
                {
                    context.AddFailure($"Sort field '{term.Field}' is not supported.");
                }
            });

        RuleFor(request => request.Filters)
            .Must(filters => filters is null || filters.Length <= ListQueryOptions.MaximumFilters)
            .WithMessage($"At most {ListQueryOptions.MaximumFilters} filters can be supplied.");

        RuleForEach(request => request.Filters)
            .Custom((filter, context) =>
            {
                if (!ListQueryParser.TryParseFilter(filter, out var term, out var error))
                {
                    context.AddFailure(error);
                    return;
                }

                if (_filterFields.Count == 0)
                {
                    return;
                }

                if (!_filterFields.TryGetValue(term.Field, out var rule))
                {
                    context.AddFailure($"Filter field '{term.Field}' is not supported.");
                    return;
                }

                if (!rule.Operators.Contains(term.Operator))
                {
                    context.AddFailure(
                        $"Filter operator '{term.Operator}' is not supported for field '{term.Field}'.");
                    return;
                }

                foreach (var value in term.Values.Where(value => !rule.IsValidValue(value)))
                {
                    context.AddFailure(
                        $"Filter value '{value}' is not valid for field '{term.Field}'.");
                }
            });
    }

    /// <summary>
    /// Restricts <see cref="ListQueryRequest.Sort" /> to the given field
    /// names (ordinal comparison). Without a call every field name is
    /// accepted and silently ignored by the query.
    /// </summary>
    protected void AllowSortFields(params string[] fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        _sortFields ??= new HashSet<string>(StringComparer.Ordinal);
        _sortFields.UnionWith(fields);
    }

    /// <summary>
    /// Allows <paramref name="field" /> in
    /// <see cref="ListQueryRequest.Filters" /> with the given operators
    /// (<see cref="FilterOperator.Equal" /> and
    /// <see cref="FilterOperator.In" /> when none are given) and any
    /// value. Once any filter field is allowed, filters naming other
    /// fields are rejected.
    /// </summary>
    protected void AllowFilterField(string field, params FilterOperator[] operators) =>
        AllowFilterField(field, _ => true, operators);

    /// <summary>
    /// Allows <paramref name="field" /> in
    /// <see cref="ListQueryRequest.Filters" /> with the given operators
    /// (<see cref="FilterOperator.Equal" /> and
    /// <see cref="FilterOperator.In" /> when none are given), accepting
    /// only values for which <paramref name="isValidValue" /> returns
    /// <see langword="true" />.
    /// </summary>
    protected void AllowFilterField(
        string field,
        Func<string, bool> isValidValue,
        params FilterOperator[] operators)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentNullException.ThrowIfNull(isValidValue);
        ArgumentNullException.ThrowIfNull(operators);

        _filterFields[field] = new FilterFieldRule(
            operators.Length == 0 ? DefaultFilterOperators : operators,
            isValidValue);
    }

    /// <summary>
    /// Allows <paramref name="field" /> with values that name a member of
    /// <typeparamref name="TEnum" /> (case-insensitive), with the given
    /// operators (<see cref="FilterOperator.Equal" /> and
    /// <see cref="FilterOperator.In" /> when none are given).
    /// </summary>
    protected void AllowEnumFilterField<TEnum>(string field, params FilterOperator[] operators)
        where TEnum : struct, Enum =>
        AllowFilterField(
            field,
            value => EnumFilter.TryParse<TEnum>(value, out _),
            operators);

    private sealed record FilterFieldRule(
        IReadOnlyCollection<FilterOperator> Operators,
        Func<string, bool> IsValidValue);
}
