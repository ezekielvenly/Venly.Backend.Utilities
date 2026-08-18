using Venly.Backend.Common;

namespace Venly.Backend.Utilities.Tests;

public class PagedListTests
{
    [Fact]
    public void Create_carries_items_and_paging_metadata()
    {
        var page = PagedList<string>.Create(["a", "b"], pageNumber: 1, pageSize: 2, totalCount: 5);

        Assert.Equal(["a", "b"], page.Items);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(5, page.TotalCount);
    }

    [Fact]
    public void HasNextPage_is_true_when_more_items_remain()
    {
        var page = PagedList<string>.Create(["a", "b"], pageNumber: 1, pageSize: 2, totalCount: 5);

        Assert.True(page.HasNextPage);
    }

    [Fact]
    public void HasNextPage_is_false_on_the_last_page()
    {
        var page = PagedList<string>.Create(["e"], pageNumber: 3, pageSize: 2, totalCount: 5);

        Assert.False(page.HasNextPage);
    }

    [Fact]
    public void HasPreviousPage_is_false_on_the_first_page_and_true_after_it()
    {
        var first = PagedList<string>.Create(["a"], pageNumber: 1, pageSize: 2, totalCount: 5);
        var second = PagedList<string>.Create(["c"], pageNumber: 2, pageSize: 2, totalCount: 5);

        Assert.False(first.HasPreviousPage);
        Assert.True(second.HasPreviousPage);
    }
}
