namespace Athenex.Whimclean.Application.Queries;

public sealed record SortTerm
{
    public SortTerm(
        string field,
        SortDirection direction = SortDirection.Ascending)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        Field = field.Trim();
        Direction = direction;
    }

    public string Field { get; }
    public SortDirection Direction { get; }
}
