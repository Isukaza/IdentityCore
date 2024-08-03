using System.Text.Json;
using StackExchange.Redis;

namespace IdentityCore.DAL.Repository.Base;

public class CacheRepositoryBase
{
    private readonly IDatabase _cache;

    public CacheRepositoryBase(IConnectionMultiplexer multiplexer)
    {
        _cache = multiplexer.GetDatabase();
    }
    
    public T Get<T>(string key)
    {
        var value = _cache.StringGet(key);
        return string.IsNullOrEmpty(value) ? default : JsonSerializer.Deserialize<T>(value);
    }
    
    public bool Add<T>(string key, T value, DateTimeOffset expirationTime)
    {
        var expiryTime = expirationTime.DateTime.Subtract(DateTime.Now);
        var json = JsonSerializer.Serialize(value);
        return _cache.StringSet(key, json, TimeSpan.FromHours(1));
    }
    
    public bool Delete(string key)
    {
        return _cache.KeyExists(key) && _cache.KeyDelete(key);
    }
}