﻿using HabitTracker.Api.Entities;

namespace HabitTracker.Api.DTOs.Habits;

public sealed record UpdateHabitDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required HabitType Type { get; init; }
    public required FrequencyDto Frequency { get; init; }
    public required TargetDto Target { get; init; }
    public DateOnly? EndDate { get; init; }
    public UpdateMileStoneDto? Milestone { get; init; }
}

public sealed record UpdateMileStoneDto
{
    public required int Target { get; init; }
}
