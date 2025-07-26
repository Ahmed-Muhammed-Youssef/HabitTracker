namespace HabitTracker.Api.DTOs.Tags;

public sealed record UpdateTagDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}
