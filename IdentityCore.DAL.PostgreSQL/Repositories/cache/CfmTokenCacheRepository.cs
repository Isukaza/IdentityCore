using IdentityCore.DAL.PostgreSQL.Models.cache.cachePrefix;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.Base;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.cache;

namespace IdentityCore.DAL.PostgreSQL.Repositories.cache;

public class CfmTokenCacheRepository : ICfmTokenCacheRepository
{
    #region C-tor

    private readonly ICacheRepositoryBase _cacheRepo;

    public CfmTokenCacheRepository(ICacheRepositoryBase cacheRepo)
    {
        _cacheRepo = cacheRepo;
    }

    #endregion

    public async Task<T> GetTokenByTokenType<T>(string key, TokenType tokenType)
    {
        var redisKey = $"{RedisPrefixes.ConfirmationToken.Prefix}:{tokenType}:{key}";
        return await _cacheRepo.GetAsync<T>(redisKey);
    }

    public bool Add<T>(string key, T value, TokenType tokenType, TimeSpan ttl)
    {
        var redisKey = $"{RedisPrefixes.ConfirmationToken.Prefix}:{tokenType}:{key}";
        return _cacheRepo.Add(redisKey, value, ttl);
    }

    public async Task<bool> DeleteAsync(string key, TokenType tokenType)
    {
        var redisKey = $"{RedisPrefixes.ConfirmationToken.Prefix}:{tokenType}:{key}";
        return await _cacheRepo.DeleteAsync(redisKey);
    }
}