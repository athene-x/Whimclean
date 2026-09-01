namespace Athenex.Whimclean.Domain.Results;

/// <summary>
/// The outcome of an operation that can fail, carrying either success or
/// an <see cref="Error" /> — a railway-style alternative to throwing for
/// expected failures. The two states are mutually exclusive by
/// construction: success always pairs with <see cref="Error.None" />,
/// failure always with a real error. Use <see cref="Result{TValue}" />
/// when the operation also produces a value.
/// </summary>
public class Result
{
    /// <summary>
    /// Whether the operation succeeded. Exactly one of this and
    /// <see cref="IsFailure" /> is <see langword="true" />.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Whether the operation failed; the negation of
    /// <see cref="IsSuccess" />. When <see langword="true" />,
    /// <see cref="Error" /> describes the failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The failure description, or <see cref="Error.None" /> on success.
    /// Never <see langword="null" />.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Initializes a result while enforcing the state invariant: success
    /// must pair with <see cref="Error.None" /> and failure with a real
    /// error.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">
    /// The error; <see cref="Error.None" /> exactly when
    /// <paramref name="isSuccess" /> is <see langword="true" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="isSuccess" /> and <paramref name="error" />
    /// disagree: a success carries a real error, or a failure carries
    /// <see cref="Error.None" />.
    /// </exception>
    protected Result(bool isSuccess, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException(
                "A successful result cannot contain an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException(
                "A failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result, carrying <see cref="Error.None" />.
    /// </summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failed result carrying <paramref name="error" />.
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
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Converts an <see cref="Error" /> into a failed result, so a method
    /// returning <see cref="Result" /> can return an error directly.
    /// Equivalent to <see cref="Failure" />.
    /// </summary>
    /// <param name="error">
    /// The failure description; must not be <see cref="Error.None" />.
    /// </param>
    public static implicit operator Result(Error error) => Failure(error);

    /// <summary>
    /// Collapses the result into a single value by invoking exactly one
    /// of the two branches: <paramref name="onSuccess" /> on success,
    /// otherwise <paramref name="onFailure" /> with <see cref="Error" />.
    /// </summary>
    /// <typeparam name="TResult">The type both branches produce.</typeparam>
    /// <param name="onSuccess">Branch invoked on success; must not be null.</param>
    /// <param name="onFailure">Branch invoked on failure; must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="onSuccess" /> or <paramref name="onFailure" /> is
    /// <see langword="null" />.
    /// </exception>
    public TResult Match<TResult>(
        Func<TResult> onSuccess,
        Func<Error, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess() : onFailure(Error);
    }
}
