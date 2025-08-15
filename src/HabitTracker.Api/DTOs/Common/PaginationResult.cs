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

    public static PaginationResult<T> Create(List<T> items, int page, int pageSize, int count)
    {
        return new PaginationResult<T> { 
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = count
        };
    }
}
