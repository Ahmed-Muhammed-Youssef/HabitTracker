using HabitTracker.Api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HabitTracker.Api.DTOs.Habits;

public sealed record HabitQueryParameters
{
    [FromQuery(Name = "q")]
    public string? Search { get; init; }
    public HabitType? Type { get; init; }
    public HabitStatus? Status { get; init; }
}
