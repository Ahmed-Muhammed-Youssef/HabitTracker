using HabitTracker.Api.Database;
using HabitTracker.Api.DTOs.Auth;
using HabitTracker.Api.DTOs.Users;
using HabitTracker.Api.Entities;
using HabitTracker.Api.Services;
using HabitTracker.Api.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace HabitTracker.Api.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(UserManager<IdentityUser> userManager,
    ApplicationIdentityDbContext identityDbContext,
    ApplicationDbContext applicationDbContext,
    TokenProvider tokenProvider,
    IOptions<JwtAuthOptions> jwtOptions) : ControllerBase
{
    private readonly JwtAuthOptions _jwtAuthOptions = jwtOptions.Value;

    [HttpPost("register")]
    public async Task<ActionResult<AccessTokensDto>> Register(RegisterUserDto userDto)
    {
        using var transaction = await identityDbContext.Database.BeginTransactionAsync();

        applicationDbContext.Database.SetDbConnection(identityDbContext.Database.GetDbConnection());
        await applicationDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());

        IdentityUser identityUser = new()
        {
            Email = userDto.Email,
            UserName = userDto.Email
        };

        IdentityResult createdResult = await userManager.CreateAsync(identityUser, userDto.Password);

        if (!createdResult.Succeeded)
        {
            var extensions = new Dictionary<string, object?>() { 
                {
                    "errors",
                    createdResult.Errors.ToDictionary(e => e.Code, e => e.Description)
                }
            };

            return Problem(detail: "unable to register user, try again",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: extensions);
        }

        IdentityResult addRoleResult = await userManager.AddToRoleAsync(identityUser, Roles.User);

        if(!addRoleResult.Succeeded)
        {
            var extensions = new Dictionary<string, object?>() {
                {
                    "errors",
                    createdResult.Errors.ToDictionary(e => e.Code, e => e.Description)
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

        AccessTokensDto accessToken = tokenProvider.Create(new TokenRequest(identityUser.Id, identityUser.Email, [Roles.User]));

        RefreshToken refreshToken = new()
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessToken.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationDays)
        };

        identityDbContext.RefreshTokens.Add(refreshToken);
        await identityDbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(accessToken);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AccessTokensDto>> Login(LoginUserDto userDto)
    {
        IdentityUser? identityUser = await userManager.FindByEmailAsync(userDto.Email);

        if (identityUser == null || !await userManager.CheckPasswordAsync(identityUser, userDto.Password))
        {
            return Unauthorized();
        }

        IList<string> roles = await userManager.GetRolesAsync(identityUser);

        AccessTokensDto accessToken = tokenProvider.Create(new TokenRequest(identityUser.Id, identityUser.Email!, roles));

        RefreshToken refreshToken = new()
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessToken.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationDays)
        };

        identityDbContext.RefreshTokens.Add(refreshToken);
        await identityDbContext.SaveChangesAsync();

        return Ok(accessToken);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AccessTokensDto>> Refresh(RefreshTokenDto refreshTokenDto)
    {
        RefreshToken? existingRefreshToken = await identityDbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshTokenDto.RefreshToken);

        if (existingRefreshToken == null || existingRefreshToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return Unauthorized();
        }

        IList<string> roles = await userManager.GetRolesAsync(existingRefreshToken.User);

        AccessTokensDto accessToken = tokenProvider.Create(new TokenRequest(existingRefreshToken.User.Id, existingRefreshToken.User.Email!, roles));
        existingRefreshToken.Token = accessToken.RefreshToken;
        existingRefreshToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationDays);
        await identityDbContext.SaveChangesAsync();
        
        return Ok(accessToken);
    }
}
