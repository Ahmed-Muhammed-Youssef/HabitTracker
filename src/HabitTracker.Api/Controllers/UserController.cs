using HabitTracker.Api.Database;
using HabitTracker.Api.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("users")]
internal sealed class UserController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(string id)
    {
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

}
