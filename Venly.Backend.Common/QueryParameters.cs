namespace Venly.Backend.Common;

public class QueryParameters
{
    private readonly int _maxPageSize = 100;
    private int _pageSize = 10;
    private string _sortOrder = "asc";

    public int PageNo { get; set; } = 1;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Min(_maxPageSize, value);
    }

    public string SortBy { get; set; } = "DateCreated";

    public string SortOrder
    {
        get => _sortOrder;
        set
        {
            if (value == "asc" || value == "desc")
                _sortOrder = value;
        }
    }
}
