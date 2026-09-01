using Athenex.Whimclean.Application.Pagination;

namespace Athenex.Whimclean.Application.Tests.Pagination;

public sealed class KeysetPagedResultTests
{
    [Fact]
    public void FinalPage_DoesNotExposeNextKey()
    {
        var result = new KeysetPagedResult<string, Guid>(["item"], 10);

        Assert.False(result.HasNextPage);
        Assert.Throws<InvalidOperationException>(() => result.NextKey);
    }

    [Fact]
    public void PageWithMoreItems_ExposesNextKey()
    {
        var nextKey = Guid.NewGuid();
        var result = new KeysetPagedResult<string, Guid>(
            ["item"],
            10,
            nextKey);

        Assert.True(result.HasNextPage);
        Assert.Equal(nextKey, result.NextKey);
    }

    [Fact]
    public void Constructor_MaterializesItems()
    {
        var source = new List<string> { "first" };
        var result = new KeysetPagedResult<string, Guid>(source, 10);

        source.Add("second");

        Assert.Equal(["first"], result.Items);
    }

    [Fact]
    public void Constructor_WhenItemCountExceedsSize_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new KeysetPagedResult<int, Guid>([1, 2], 1));
    }

    [Fact]
    public void Constructor_WithNullItemElement_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new KeysetPagedResult<string, Guid>(["first", null!], 10));
    }
}
