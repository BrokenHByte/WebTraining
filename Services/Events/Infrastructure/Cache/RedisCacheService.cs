using System.Text.Json;
using Events.Application.Abstractions.Persistence.Services;
using Events.Application.Events.Queries.GetEventById;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Events.Infrastructure.Cache;

public class RedisCacheService(IConnectionMultiplexer redis, IOptions<RedisConfig> config, ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly IDatabase _cacheDb = redis.GetDatabase();
    private readonly int _defaultTTL = config.Value.DefaultTTLMinutes;

    public async Task SetObjectJson<T>(string key, T objectJson, int? ttl = null)
    {
        if (!redis.IsConnected)
        {
            logger.LogError("Redis is unavailable");
            return;
        }

        var json = JsonSerializer.Serialize(objectJson);
        await _cacheDb.StringSetAsync(key, json, TimeSpan.FromMinutes(ttl ?? _defaultTTL));
    }

    public async Task<T?> GetObjectJson<T>(string key)
    {
        if (!redis.IsConnected)
        {
            logger.LogError("Redis is unavailable");
            return default;
        }

        var redisValue = await _cacheDb.StringGetAsync(key);
        if (!redisValue.HasValue)
            return default;

        var obj = JsonSerializer.Deserialize<T>(redisValue.ToString());
        if (obj != null)
            return obj;
        return default;
    }

    public async Task DeleteObjectJson(string key)
    {
        if (!redis.IsConnected)
        {
            logger.LogError("Redis is unavailable");
            return;
        }
        await _cacheDb.KeyDeleteAsync(key);
    }
}