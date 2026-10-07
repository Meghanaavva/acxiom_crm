using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Helpers;

// Common list query parameters: ?page=1&pageSize=10&search=&sortBy=&sortOrder=asc
public class PageQuery
{
    private int _page = 1;
    private int _pageSize = 10;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, 100);
    }

    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }

    public bool Ascending => string.Equals(SortOrder, "asc", StringComparison.OrdinalIgnoreCase);
    public int Skip => (Page - 1) * PageSize;
}

public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

public static class PagingExtensions
{
    public static async Task<PagedResult<TDto>> ToPagedResultAsync<TEntity, TDto>(
        this IQueryable<TEntity> query, PageQuery page, Func<TEntity, TDto> map)
    {
        var total = await query.CountAsync();
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync();
        return new PagedResult<TDto>
        {
            Data = items.Select(map).ToList(),
            Pagination = new PaginationInfo
            {
                Page = page.Page,
                PageSize = page.PageSize,
                Total = total,
                TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)page.PageSize)),
            },
        };
    }

    // Sort by a whitelisted column name. Unknown names fall back to the default sort.
    public static IQueryable<T> SortBy<T>(this IQueryable<T> query, PageQuery page,
        Dictionary<string, System.Linq.Expressions.Expression<Func<T, object?>>> columns, string defaultColumn, bool defaultAscending = false)
    {
        var useDefault = string.IsNullOrEmpty(page.SortBy) || !columns.ContainsKey(page.SortBy);
        var key = useDefault ? defaultColumn : page.SortBy!;
        var ascending = useDefault && string.IsNullOrEmpty(page.SortOrder) ? defaultAscending : page.Ascending;
        return ascending ? query.OrderBy(columns[key]) : query.OrderByDescending(columns[key]);
    }
}
