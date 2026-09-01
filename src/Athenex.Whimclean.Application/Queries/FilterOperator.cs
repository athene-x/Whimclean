namespace Athenex.Whimclean.Application.Queries;

/// <summary>
/// Comparison a <see cref="FilterTerm" /> applies between its field and its
/// value.
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
    LessThanOrEqual = 4
}
