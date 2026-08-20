namespace Athenex.Whimclean.Application.Queries;

public sealed record FilterTerm
{
    public FilterTerm(
        string field,
        string value,
        FilterOperator @operator = FilterOperator.Equal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentNullException.ThrowIfNull(value);

        Field = field.Trim();
        Value = value.Trim();
        Operator = @operator;
    }

    public string Field { get; }
    public string Value { get; }
    public FilterOperator Operator { get; }
}
