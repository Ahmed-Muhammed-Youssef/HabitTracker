using HabitTracker.Api.DTOs.Common;

namespace HabitTracker.Api.DTOs.Tags;

public sealed record TagsCollectionDto : ICollectionResponse<TagDto>
{
    public List<TagDto> Items { get; init; }
}
