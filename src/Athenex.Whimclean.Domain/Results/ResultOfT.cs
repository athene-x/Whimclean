namespace Athenex.Whimclean.Domain.Results;

/// <summary>
/// A <see cref="Result" /> that also carries a value on success. Failure
/// carries only the <see cref="Result.Error" />; the value is reachable
/// solely through <see cref="Value" />, <see cref="Match{TResult}" />,
/// <see cref="Map{TResult}" />, or <see cref="Bind{TResult}" />, which
/// together form the composition surface.
/// </summary>
/// <typeparam name="TValue">
/// The value produced on success; constrained to non-nullable types so a
/// successful result always holds a real value.
/// </typeparam>
public sealed class Result<TValue> : Result
    where TValue : notnull
{
    private readonly TValue? _value;

    /// <summary>
    /// The value produced by the successful operation; never
    /// <see langword="null" />. Check <see cref="Result.IsSuccess" />
    /// first, or prefer <see cref="Match{TResult}" /> to branch safely.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The result is a failure — a failed result has no value.
    /// </exception>
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

    /// <summary>
    /// Creates a successful result carrying <paramref name="value" />.
    /// </summary>
    /// <param name="value">The produced value; must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static Result<TValue> Success(TValue value) => new(value);

    /// <summary>
    /// Creates a failed result carrying <paramref name="error" /> and no
    /// value.
    /// </summary>
    /// <param name="error">
    /// The failure description; must not be <see cref="Error.None" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="error" /> is <see cref="Error.None" />.
    /// </exception>
    public static new Result<TValue> Failure(Error error) => new(error);

    /// <summary>
    /// Converts a value into a successful result, so a method returning
    /// <see cref="Result{TValue}" /> can return the value directly.
    /// Equivalent to <see cref="Success" />.
    /// </summary>
    /// <param name="value">The produced value; must not be null.</param>
    public static implicit operator Result<TValue>(TValue value) =>
        Success(value);

    /// <summary>
    /// Converts an <see cref="Error" /> into a failed result, so a method
    /// returning <see cref="Result{TValue}" /> can return an error
    /// directly. Equivalent to <see cref="Failure" />.
    /// </summary>
    /// <param name="error">
    /// The failure description; must not be <see cref="Error.None" />.
    /// </param>
    public static implicit operator Result<TValue>(Error error) =>
        Failure(error);

    /// <summary>
    /// Collapses the result into a single value by invoking exactly one
    /// of the two branches: <paramref name="onSuccess" /> with
    /// <see cref="Value" /> on success, otherwise
    /// <paramref name="onFailure" /> with <see cref="Result.Error" />.
    /// </summary>
    /// <typeparam name="TResult">The type both branches produce.</typeparam>
    /// <param name="onSuccess">Branch invoked on success; must not be null.</param>
    /// <param name="onFailure">Branch invoked on failure; must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="onSuccess" /> or <paramref name="onFailure" /> is
    /// <see langword="null" />.
    /// </exception>
    public TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<Error, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }

    /// <summary>
    /// Transforms the value of a successful result with
    /// <paramref name="map" />; a failure passes through unchanged with
    /// its error, and <paramref name="map" /> is never invoked.
    /// </summary>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="map">The transformation; must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="map" /> is <see langword="null" />.
    /// </exception>
    public Result<TResult> Map<TResult>(Func<TValue, TResult> map)
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(map);

        return IsSuccess
            ? Result<TResult>.Success(map(Value))
            : Result<TResult>.Failure(Error);
    }

    /// <summary>
    /// Chains a further result-producing operation: on success, returns
    /// what <paramref name="bind" /> produces from the value; a failure
    /// passes through unchanged with its error, and
    /// <paramref name="bind" /> is never invoked. Use instead of
    /// <see cref="Map{TResult}" /> when the next step can itself fail.
    /// </summary>
    /// <typeparam name="TResult">The value type of the chained result.</typeparam>
    /// <param name="bind">The next operation; must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="bind" /> is <see langword="null" />.
    /// </exception>
    public Result<TResult> Bind<TResult>(Func<TValue, Result<TResult>> bind)
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(bind);

        return IsSuccess ? bind(Value) : Result<TResult>.Failure(Error);
    }
}
