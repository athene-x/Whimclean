using Athenex.Whimclean.Application.Pagination;

namespace Athenex.Whimclean.Application.Tests.Pagination;

public sealed class OffsetPagedResultTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    [InlineData(41, 3)]
    public void Constructor_CalculatesTotalPages(
        long totalItems,
        long expectedPages)
    {
        var result = new OffsetPagedResult<int>(
            [],
            page: 1,
            size: 20,
            totalItems);

        Assert.Equal(expectedPages, result.TotalPages);
    }

    [Fact]
    public void Constructor_MaterializesItems()
    {
        var source = new List<int> { 1, 2 };
        var result = new OffsetPagedResult<int>(source, 1, 10, 2);

        source.Add(3);

        Assert.Equal([1, 2], result.Items);
    }

    [Fact]
    public void Constructor_WhenItemCountExceedsSize_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new OffsetPagedResult<int>([1, 2], 1, 1, 2));
    }

    [Fact]
    public void Constructor_WhenItemCountExceedsTotal_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new OffsetPagedResult<int>([1, 2], 1, 10, 1));
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 10, -1)]
    public void Constructor_WithInvalidRange_ThrowsArgumentOutOfRangeException(
        int page,
        int size,
        long totalItems)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OffsetPagedResult<int>([], page, size, totalItems));
    }
}
