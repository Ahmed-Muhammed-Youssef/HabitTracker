namespace HabitTracker.Api.DTOs.Auth;

public sealed record TokenRequest(string UserId, string UserEmail, IEnumerable<string> Roels);
