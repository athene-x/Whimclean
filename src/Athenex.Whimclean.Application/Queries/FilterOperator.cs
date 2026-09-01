namespace Athenex.Whimclean.Application.Queries;

/// <summary>
/// Comparison a <see cref="FilterTerm" /> applies between its field and its
/// value (or values, for <see cref="In" />).
/// </summary>
public enum FilterOperator
{
    /// <summary>The field must equal the term's value.</summary>
    Equal = 0,

    /// <summary>The field must exceed the term's value.</summary>
    GreaterThan = 1,

    /// <summary>The field must equal or exceed the term's value.</summary>
    GreaterThanOrEqual = 2,

    /// <summary>The field must fall below the term's value.</summary>
    LessThan = 3,

    /// <summary>
    /// The field must equal or fall below the term's value.
    /// </summary>
    LessThanOrEqual = 4,

    /// <summary>The field must differ from the term's value.</summary>
    NotEqual = 5,

    /// <summary>
    /// The field's text must contain the term's value.
    /// </summary>
    Contains = 6,

    /// <summary>
    /// The field's text must start with the term's value.
    /// </summary>
    StartsWith = 7,

    /// <summary>
    /// The field's text must end with the term's value.
    /// </summary>
    EndsWith = 8,

    /// <summary>
    /// The field must equal at least one of the term's values.
    /// </summary>
    In = 9
}
