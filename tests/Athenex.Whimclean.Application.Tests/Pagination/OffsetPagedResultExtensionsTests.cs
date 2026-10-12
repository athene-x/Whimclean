using Athenex.Whimclean.Application.Pagination;

namespace Athenex.Whimclean.Application.Tests.Pagination;

public sealed class OffsetPagedResultExtensionsTests
{
    [Fact]
    public void Map_ProjectsItemsAndKeepsTotals()
    {
        var page = new OffsetPagedResult<int>([1, 2], page: 3, size: 2, totalItems: 10);

        var mapped = page.Map(item => item.ToString());

        Assert.Equal(["1", "2"], mapped.Items);
        Assert.Equal(3, mapped.Page);
        Assert.Equal(2, mapped.Size);
        Assert.Equal(10, mapped.TotalItems);
    }
}
