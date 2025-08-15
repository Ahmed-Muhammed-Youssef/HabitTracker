using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Api.DTOs.Common;

public sealed record PaginationResult<T> : ICollectionResponse<T>
{
    public List<T> Items { get; init; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; init; }

    public int TotalPages => (int) Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalCount;

    /// <summary>
    /// Creates a pagination result instance
    /// </summary>
    /// <param name="query">the ef iqueryable</param>
    /// <param name="page">page number starts with 1</param>
    /// <param name="pageSize">number of items per page</param>
    /// <returns>pagination result with the items and the items total number</returns>
    public static async Task<PaginationResult<T>> CreateAsync(IQueryable<T> query, int page, int pageSize)
    {
        int totalCount = await query.CountAsync();

        List<T> items = await query.Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginationResult<T> { 
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount 
        };
    }
}
