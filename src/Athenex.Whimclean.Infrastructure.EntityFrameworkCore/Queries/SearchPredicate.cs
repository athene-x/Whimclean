using System.Linq.Expressions;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries;

/// <summary>
/// Composes a store-translatable predicate that matches a search pattern
/// against any of several string members. The provider packages use it
/// to offer <c>Search…</c> helpers over their own matching function.
/// </summary>
public static class SearchPredicate
{
    /// <summary>
    /// Builds <c>match(field1, pattern) || match(field2, pattern) || …</c>
    /// by inlining <paramref name="match" /> once per selector, so the
    /// result contains no invocation nodes and translates like a
    /// hand-written lambda. The pattern is referenced as a captured value
    /// rather than a literal, so the provider sends it as a query
    /// parameter and reuses the compiled query across search terms.
    /// </summary>
    /// <param name="match">
    /// The matching function, taking the member value and the pattern;
    /// for example <c>(value, pattern) =&gt; EF.Functions.Like(value, pattern, "\\")</c>.
    /// </param>
    /// <param name="pattern">The pattern to match every field against.</param>
    /// <param name="fields">Selectors of the string members to search.</param>
    /// <exception cref="ArgumentNullException">
    /// Any argument is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fields" /> is empty.
    /// </exception>
    public static Expression<Func<T, bool>> AnyMatch<T>(
        Expression<Func<string?, string, bool>> match,
        string pattern,
        params Expression<Func<T, string?>>[] fields)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(fields);

        if (fields.Length == 0)
        {
            throw new ArgumentException(
                "At least one field selector is required.",
                nameof(fields));
        }

        var parameter = Expression.Parameter(typeof(T), "entity");
        var patternAccess = Expression.Property(
            Expression.Constant(new PatternHolder(pattern)),
            nameof(PatternHolder.Pattern));

        var body = fields
            .Select(field =>
            {
                ArgumentNullException.ThrowIfNull(field, nameof(fields));

                var fieldBody = new ParameterReplacer((field.Parameters[0], parameter))
                    .Visit(field.Body);

                return new ParameterReplacer(
                        (match.Parameters[0], fieldBody),
                        (match.Parameters[1], patternAccess))
                    .Visit(match.Body);
            })
            .Aggregate(Expression.OrElse);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    /// <summary>
    /// Holds the pattern the way a compiler-generated closure would, so
    /// the query pipeline treats the member access as a parameter.
    /// </summary>
    private sealed class PatternHolder(string pattern)
    {
        public string Pattern { get; } = pattern;
    }

    private sealed class ParameterReplacer(
        params (ParameterExpression Parameter, Expression Replacement)[] replacements)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            foreach (var (parameter, replacement) in replacements)
            {
                if (node == parameter)
                {
                    return replacement;
                }
            }

            return base.VisitParameter(node);
        }
    }
}
