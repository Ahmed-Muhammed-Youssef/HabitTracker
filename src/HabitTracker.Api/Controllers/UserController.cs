using System.Security.Claims;
using HabitTracker.Api.Database;
using HabitTracker.Api.DTOs.Users;
using HabitTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("users")]
internal sealed class UserController(ApplicationDbContext dbContext, UserContext userContext) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(string id)
    {
        var userId = await userContext.GetUserIdAsync();

        if (userId != id)
        {
            return Forbid();
        }

        UserDto? userDto = await dbContext.Users.
            Where(u => u.Id == id)
            .Select(UserQueries.ProjectToDto())
            .FirstOrDefaultAsync();

        if (userDto == null)
        {
            return NotFound();
        }

        return Ok(userDto);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = await userContext.GetUserIdAsync();

        if(userId == null)
        {
            return Unauthorized();
        }

        UserDto? userDto = await dbContext.Users.
            Where(u => u.Id == userId)
            .Select(UserQueries.ProjectToDto())
            .FirstOrDefaultAsync();

        if (userDto == null)
        {
            return NotFound();
        }

        return Ok(userDto);
    }

}
