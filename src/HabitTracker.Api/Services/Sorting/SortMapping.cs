namespace HabitTracker.Api.Services.Sorting;

public sealed record SortMapping(string SortField, string PropertyName, bool Reverse = false);
