namespace Athenex.Whimclean.Domain.Results;

public sealed record Error
{
    private Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    public string Code { get; }
    public string Description { get; }
    public ErrorType Type { get; }

    public static Error None { get; } =
        new(string.Empty, string.Empty, ErrorType.None);

    public static Error Failure(string code, string description) =>
        Create(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description) =>
        Create(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) =>
        Create(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        Create(code, description, ErrorType.Conflict);

    private static Error Create(
        string code,
        string description,
        ErrorType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (type == ErrorType.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                "A regular error cannot use the None type.");
        }

        return new Error(code, description, type);
    }
}
