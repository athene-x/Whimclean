using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL.Queries;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL.Tests.Queries;

public sealed class PostgreSqlSearchQueryableExtensionsTests
{
    private sealed record Person(string Name, string? Email);

    [Fact]
    public void SearchILike_BuildsAnILikePredicatePerField()
    {
        var query = new[] { new Person("a", null) }.AsQueryable();

        var expression = query
            .SearchILike("50%", person => person.Name, person => person.Email)
            .Expression
            .ToString();

        Assert.Equal(2, expression.Split("ILike").Length - 1);
        Assert.Contains("OrElse", expression);

        // The pattern is captured, not inlined, so the provider sends it as
        // a parameter and reuses the compiled query across search terms.
        Assert.DoesNotContain("%50", expression);
    }

    [Fact]
    public void SearchILike_WithNullTerm_LeavesQueryUnchanged()
    {
        var query = new[] { new Person("a", null) }.AsQueryable();

        Assert.Same(query, query.SearchILike(null, person => person.Name));
    }
}
