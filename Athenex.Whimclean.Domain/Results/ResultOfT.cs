namespace Athenex.Whimclean.Domain.Results;

public sealed class Result<TValue> : Result
    where TValue : notnull
{
    private readonly TValue? _value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            "The value of a failed result cannot be accessed.");

    private Result(TValue value)
        : base(true, Error.None)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    private Result(Error error)
        : base(false, error)
    {
    }

    public static Result<TValue> Success(TValue value) => new(value);

    public static new Result<TValue> Failure(Error error) => new(error);
}
