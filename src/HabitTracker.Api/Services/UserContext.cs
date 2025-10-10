using HabitTracker.Api.Database;
using HabitTracker.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HabitTracker.Api.Services;

public class UserContext(IHttpContextAccessor httpContextAccessor,
    ApplicationDbContext dbContext,
    IMemoryCache memoryCache)
{
    private const string UserCacheKeyPrefix = "users:id:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public async Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        var identityId = httpContextAccessor.HttpContext?.User?.GetIdentityId();
        if (identityId == null)
        {
            return null;
        }
        string cacheKey = $"{UserCacheKeyPrefix}{identityId}";
        if (memoryCache.TryGetValue(cacheKey, out string? cachedUserId))
        {
            return cachedUserId;
        }
        var userId = await dbContext.Users
            .Where(u => u.IdentityId == identityId)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (userId != null)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(CacheDuration);
            memoryCache.Set(cacheKey, userId, cacheEntryOptions);
        }
        return userId;
    }
}
