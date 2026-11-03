namespace NHS111.Infrastructure.Security;

using NHS111.Domain.Interfaces;
using StackExchange.Redis;

public class RedisTokenBlacklistService(IConnectionMultiplexer redis) : ITokenBlacklistService
{
    private readonly IDatabase _redisDb = redis.GetDatabase();

    public async Task BlacklistTokenAsync(string token, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        try
        {
            var expiry = expiration ?? TimeSpan.FromHours(24);
            await _redisDb.StringSetAsync($"blacklist:{token}", "1", expiry);
        }
        catch
        {
            // Redis error should not fail the operation if Redis is temporarily unreachable
        }
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken ct = default)
    {
        try
        {
            return await _redisDb.KeyExistsAsync($"blacklist:{token}");
        }
        catch
        {
            return false;
        }
    }
}
