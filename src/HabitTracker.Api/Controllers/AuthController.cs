using HabitTracker.Api.Database;
using HabitTracker.Api.DTOs.Auth;
using HabitTracker.Api.DTOs.Users;
using HabitTracker.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HabitTracker.Api.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(UserManager<IdentityUser> userManager,
    ApplicationIdentityDbContext identityDbContext,
    ApplicationDbContext applicationDbContext) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserDto userDto)
    {
        using var transaction = await identityDbContext.Database.BeginTransactionAsync();

        applicationDbContext.Database.SetDbConnection(identityDbContext.Database.GetDbConnection());
        await applicationDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());

        IdentityUser identityUser = new()
        {
            Email = userDto.Email,
            UserName = userDto.Email
        };

        var identityResult = await userManager.CreateAsync(identityUser, userDto.Password);

        if (!identityResult.Succeeded)
        {
            var extensions = new Dictionary<string, object?>() { 
                {
                    "errors",
                    identityResult.Errors.ToDictionary(e => e.Code, e => e.Description)
                }
            };

            return Problem(detail: "unable to register user, try again",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: extensions);
        }

        User user = userDto.ToEntity();

        user.IdentityId = identityUser.Id;

        applicationDbContext.Users.Add(user);

        await applicationDbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(user.Id);
    }
}
