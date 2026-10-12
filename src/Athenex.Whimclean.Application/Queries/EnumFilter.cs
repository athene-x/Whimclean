namespace Athenex.Whimclean.Application.Queries;

/// <summary>
/// Parses filter values into enum members for translation into store
/// predicates; unknown names are ignored rather than failing the query,
/// since the API validator has already rejected them.
/// </summary>
public static class EnumFilter
{
    /// <summary>
    /// Parses <paramref name="value" /> case-insensitively into a defined
    /// member of <typeparamref name="TEnum" />.
    /// </summary>
    public static bool TryParse<TEnum>(string value, out TEnum result)
        where TEnum : struct, Enum =>
        Enum.TryParse(value, true, out result) && Enum.IsDefined(result);

    /// <summary>
    /// Parses every value of <paramref name="values" /> that names a
    /// defined member of <typeparamref name="TEnum" />, skipping the
    /// rest. The result is a list so it translates to an <c>IN</c>
    /// clause.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="values" /> is <see langword="null" />.
    /// </exception>
    public static List<TEnum> Parse<TEnum>(IEnumerable<string> values)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(values);

        var results = new List<TEnum>();

        foreach (var value in values)
        {
            if (TryParse<TEnum>(value, out var result))
            {
                results.Add(result);
            }
        }

        return results;
    }
}
