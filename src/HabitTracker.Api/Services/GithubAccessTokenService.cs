using HabitTracker.Api.Database;
using HabitTracker.Api.DTOs.Github;
using HabitTracker.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Api.Services;

public sealed class GithubAccessTokenService(ApplicationDbContext dbContext)
{
    public async Task StoreAsync(string userId, StoreGitHubAccessToken accessTokenDto, CancellationToken cancellationToken = default)
    {
        GithubAccessToken? exestinToken = await GetAccessTokenAsync(userId, cancellationToken);

        if (exestinToken is not null)
        {
            exestinToken.Token = accessTokenDto.AccessToken;
            exestinToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(accessTokenDto.ExpiresInDays);
        }
        else
        {
            GithubAccessToken newToken = new ()
            {
                Id = $"gh_{Guid.CreateVersion7()}",
                UserId = userId,
                Token = accessTokenDto.AccessToken,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(accessTokenDto.ExpiresInDays)
            };

            dbContext.GithubAccessTokens.Add(newToken);

        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        GithubAccessToken token = await GetAccessTokenAsync(userId, cancellationToken);
        
        return token?.Token;
    }

    public async Task RevokeAsync(string userId, CancellationToken cancellationToken = default)
    {
        GithubAccessToken? token = await GetAccessTokenAsync(userId, cancellationToken);
        if(token is null)
        {
            return;
        }
        dbContext.GithubAccessTokens.Remove(token);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<GithubAccessToken?> GetAccessTokenAsync(string userId, CancellationToken cancellationToken)
    {
        GithubAccessToken token = await dbContext.GithubAccessTokens
            .Where(t => t.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        return token;
    }
}
