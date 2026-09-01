namespace Athenex.Whimclean.Domain.Results;

/// <summary>
/// An immutable description of why an operation failed, carried by a
/// failed <see cref="Result" />. Instances are created only through the
/// static factories, which guarantee a non-blank <see cref="Code" /> and
/// <see cref="Description" />; the sole exception is the
/// <see cref="None" /> sentinel, which pairs exclusively with success.
/// </summary>
public sealed record Error
{
    private Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    /// <summary>
    /// A stable, machine-readable identifier for the failure (e.g.
    /// <c>"Order.NotFound"</c>). Never blank except on
    /// <see cref="None" />.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// A human-readable explanation of the failure. Never blank except on
    /// <see cref="None" />.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The semantic category of the failure, used by outer layers to
    /// decide how to surface it. <see cref="ErrorType.None" /> only on
    /// the <see cref="None" /> sentinel.
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    /// The "no error" sentinel carried by successful results. It is the
    /// only instance whose <see cref="Type" /> is
    /// <see cref="ErrorType.None" />, and it never describes a failure.
    /// </summary>
    public static Error None { get; } =
        new(string.Empty, string.Empty, ErrorType.None);

    /// <summary>
    /// Creates an error for an unexpected or unclassified fault — the
    /// category of last resort when no more specific factory applies.
    /// </summary>
    /// <param name="code">Stable failure identifier; must not be blank.</param>
    /// <param name="description">Human-readable explanation; must not be blank.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="code" /> or <paramref name="description" /> is
    /// null, empty, or whitespace.
    /// </exception>
    public static Error Failure(string code, string description) =>
        Create(code, description, ErrorType.Failure);

    /// <summary>
    /// Creates an error meaning the input was malformed or violated a
    /// business rule, so the operation was never attempted.
    /// </summary>
    /// <param name="code">Stable failure identifier; must not be blank.</param>
    /// <param name="description">Human-readable explanation; must not be blank.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="code" /> or <paramref name="description" /> is
    /// null, empty, or whitespace.
    /// </exception>
    public static Error Validation(string code, string description) =>
        Create(code, description, ErrorType.Validation);

    /// <summary>
    /// Creates an error meaning the requested resource does not exist or
    /// is not visible to the caller.
    /// </summary>
    /// <param name="code">Stable failure identifier; must not be blank.</param>
    /// <param name="description">Human-readable explanation; must not be blank.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="code" /> or <paramref name="description" /> is
    /// null, empty, or whitespace.
    /// </exception>
    public static Error NotFound(string code, string description) =>
        Create(code, description, ErrorType.NotFound);

    /// <summary>
    /// Creates an error meaning the operation clashes with the current
    /// state of the system (e.g. a duplicate, or a concurrent change).
    /// </summary>
    /// <param name="code">Stable failure identifier; must not be blank.</param>
    /// <param name="description">Human-readable explanation; must not be blank.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="code" /> or <paramref name="description" /> is
    /// null, empty, or whitespace.
    /// </exception>
    public static Error Conflict(string code, string description) =>
        Create(code, description, ErrorType.Conflict);

    /// <summary>
    /// Creates an error meaning the caller lacks a valid identity — who
    /// they are could not be established.
    /// </summary>
    /// <param name="code">Stable failure identifier; must not be blank.</param>
    /// <param name="description">Human-readable explanation; must not be blank.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="code" /> or <paramref name="description" /> is
    /// null, empty, or whitespace.
    /// </exception>
    public static Error Unauthorized(string code, string description) =>
        Create(code, description, ErrorType.Unauthorized);

    /// <summary>
    /// Creates an error meaning the caller's identity is known but they
    /// are not permitted to perform the operation.
    /// </summary>
    /// <param name="code">Stable failure identifier; must not be blank.</param>
    /// <param name="description">Human-readable explanation; must not be blank.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="code" /> or <paramref name="description" /> is
    /// null, empty, or whitespace.
    /// </exception>
    public static Error Forbidden(string code, string description) =>
        Create(code, description, ErrorType.Forbidden);

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
