using System.Text.Json;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces;
using StackExchange.Redis;

namespace IdentityCore.DAL.PostgreSQL.Repositories;

public class CacheRepository : ICacheRepository
{
    private readonly IDatabase _cache;

    public CacheRepository(IConnectionMultiplexer multiplexer)
    {
        _cache = multiplexer.GetDatabase();
    }

    public async Task<T> GetAsync<T>(string key)
    {
        var value = await _cache.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value) : default;
    }

    public bool Add<T>(string key, T value, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(value);
        return _cache.StringSet(key, json, ttl);
    }

    public async Task<bool> UpdateAsync<T>(string key, T value, TimeSpan expiry)
    {
        var propertyInfo = typeof(T).GetProperty("Modified");
        if (propertyInfo != null && propertyInfo.CanWrite)
            propertyInfo.SetValue(value, DateTime.UtcNow);

        var json = JsonSerializer.Serialize(value);
        return await _cache.StringSetAsync(key, json, expiry);
    }

    public async Task<bool> UpdateTtlAsync(string key, TimeSpan ttl) =>
        await _cache.KeyExpireAsync(key, ttl);

    public async Task<bool> DeleteAsync(string key) =>
        await _cache.KeyDeleteAsync(key);

    public async Task<bool> KeyExistsAsync(string key) =>
        await _cache.KeyExistsAsync(key);
}