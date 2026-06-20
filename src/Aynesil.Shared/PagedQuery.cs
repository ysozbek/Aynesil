namespace Aynesil.Shared;

/// <summary>
/// Base class for list queries that support pagination, sorting, and search.
/// All list query handlers should accept a type derived from this class.
/// </summary>
public abstract class PagedQuery
{
    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : value > 200 ? 200 : value;
    }

    /// <summary>Free-text search term.</summary>
    public string? Search { get; set; }

    /// <summary>Column to sort by (camelCase property name).</summary>
    public string? SortBy { get; set; }

    /// <summary>Sort direction: 'asc' (default) or 'desc'.</summary>
    public string SortDirection { get; set; } = "asc";

    public bool IsDescending => SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

    public int Skip => (Page - 1) * PageSize;
}
