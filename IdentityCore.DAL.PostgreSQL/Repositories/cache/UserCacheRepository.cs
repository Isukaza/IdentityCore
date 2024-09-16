using IdentityCore.DAL.PostgreSQL.Models.cache.cachePrefix;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.Base;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.cache;

namespace IdentityCore.DAL.PostgreSQL.Repositories.cache;

public class UserCacheRepository : IUserCacheRepository
{
    private readonly ICacheRepositoryBase _cacheRepo;

    public UserCacheRepository(ICacheRepositoryBase cacheRepo)
    {
        _cacheRepo = cacheRepo;
    }

    public async Task<T> GetUserByIdAsync<T>(string prefix, Guid id)
    {
        var key = $"{prefix}:{id}";
        return await _cacheRepo.GetAsync<T>(key);
    }

    public bool AddMappingToCache(string keyPrefix, string value, TimeSpan ttl)
    {
        var key = $"{keyPrefix}:{value}";
        return _cacheRepo.Add(key, value, ttl);
    }

    public bool AddEntityToCache<T>(string keyPrefix, Guid id, T entity, TimeSpan ttl)
    {
        var key = $"{keyPrefix}:{id}";
        return _cacheRepo.Add(key, entity, ttl);
    }

    public async Task<bool> UpdateTtlEntityInCache(string keyPrefix, string id, TimeSpan ttl)
    {
        var key = $"{keyPrefix}:{id}";
        return await _cacheRepo.UpdateTtlAsync(key, ttl);
    }

    public Task<bool> RemoveEntityFromCache(string keyPrefix, string value)
    {
        var key = $"{keyPrefix}:{value}";
        return _cacheRepo.DeleteAsync(key);
    }

    public async Task<bool> IsUserUpdateInProgressAsync(Guid id)
    {
        var key = $"{RedisPrefixes.User.Update}:{id}";
        return await _cacheRepo.KeyExistsAsync(key);
    }

    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        var keyEmail = $"{RedisPrefixes.User.Email}:{email}";
        var doesUserExistInRedis = !string.IsNullOrEmpty(await _cacheRepo.GetAsync<string>(keyEmail));
        return doesUserExistInRedis;
    }

    public async Task<bool> UserExistsByUsernameAsync(string username)
    {
        var keyUsername = $"{RedisPrefixes.User.Name}:{username}";
        var doesUserExistInRedis = !string.IsNullOrEmpty(await _cacheRepo.GetAsync<string>(keyUsername));
        return doesUserExistInRedis;
    }
}