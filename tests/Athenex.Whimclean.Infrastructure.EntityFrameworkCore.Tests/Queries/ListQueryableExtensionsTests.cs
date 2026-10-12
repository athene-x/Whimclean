using Athenex.Whimclean.Application.Queries;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries;
using Microsoft.EntityFrameworkCore;

namespace Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Tests.Queries;

public sealed class ListQueryableExtensionsTests
{
    private sealed class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    private sealed class PeopleContext(DbContextOptions<PeopleContext> options) : DbContext(options)
    {
        public DbSet<Person> People => Set<Person>();
    }

    private static async Task<PeopleContext> CreateContextAsync()
    {
        var context = new PeopleContext(new DbContextOptionsBuilder<PeopleContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        context.People.AddRange(
            new Person { Id = 1, Name = "Alice", Email = "alice@example.com" },
            new Person { Id = 2, Name = "Bob", Email = "bob@example.com" },
            new Person { Id = 3, Name = "100% sure", Email = null },
            new Person { Id = 4, Name = "Carol", Email = "carol@sample.org" });
        await context.SaveChangesAsync();

        return context;
    }

    [Fact]
    public async Task SearchLike_MatchesAnyFieldAndEscapesWildcards()
    {
        await using var context = await CreateContextAsync();

        var byEmailDomain = await context.People
            .SearchLike("example", person => person.Name, person => person.Email)
            .Select(person => person.Id)
            .ToListAsync();
        var literalPercent = await context.People
            .SearchLike("100%", person => person.Name, person => person.Email)
            .Select(person => person.Id)
            .ToListAsync();

        Assert.Equal([1, 2], byEmailDomain);
        Assert.Equal([3], literalPercent);
    }

    [Fact]
    public async Task SearchLike_WithNullTerm_LeavesQueryUnchanged()
    {
        await using var context = await CreateContextAsync();

        var all = await context.People.SearchLike(null, person => person.Name).CountAsync();

        Assert.Equal(4, all);
    }

    [Fact]
    public async Task Page_And_ToPage_ProduceTheRequestedSlice()
    {
        await using var context = await CreateContextAsync();
        var options = new ListQueryOptions(page: 2, size: 3);
        var query = context.People.OrderBy(person => person.Id);

        var total = await query.LongCountAsync();
        var items = await query.Page(options).ToListAsync();
        var page = items.ToPage(options, total);

        Assert.Equal([4], page.Items.Select(person => person.Id));
        Assert.Equal(2, page.Page);
        Assert.Equal(3, page.Size);
        Assert.Equal(4, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
    }

    [Fact]
    public void SearchPredicate_RequiresAField()
    {
        Assert.Throws<ArgumentException>(() =>
            SearchPredicate.AnyMatch<Person>((value, pattern) => value == pattern, "x"));
    }
}
