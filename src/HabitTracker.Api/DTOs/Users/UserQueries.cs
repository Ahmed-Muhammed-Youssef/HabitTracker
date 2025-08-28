using System.Linq.Expressions;
using HabitTracker.Api.Entities;

namespace HabitTracker.Api.DTOs.Users;

internal static class UserQueries
{
    public static Expression<Func<User, UserDto>> ProjectToDto()
    {
        return u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            CreatedAtUtc = u.CreatedAtUtc,
            UpdatedAtUtc = u.UpdatedAtUtc
        };
    }
}
