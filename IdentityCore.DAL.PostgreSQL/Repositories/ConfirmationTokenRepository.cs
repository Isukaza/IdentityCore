using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.cache.cachePrefix;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces;

namespace IdentityCore.DAL.PostgreSQL.Repositories;

public class ConfirmationTokenRepository : IConfirmationTokenRepository
{
    #region C-tor

    private readonly ICacheRepository _cacheRepo;

    public ConfirmationTokenRepository(ICacheRepository cacheRepo)
    {
        _cacheRepo = cacheRepo;
    }

    #endregion

    public async Task<RedisConfirmationToken> GetFromRedisAsync(string key, TokenType tokenType)
    {
        var keyRedis = $"{RedisPrefixes.ConfirmationToken.Prefix}:{tokenType}:{key}";
        var tokenDb = await _cacheRepo.GetAsync<RedisConfirmationToken>(keyRedis);
        return tokenDb != null && tokenDb.TokenType == tokenType ? tokenDb : null;
    }

    public async Task<RedisConfirmationToken> GetFromByUserIdRedisAsync(Guid userId, TokenType tokenType)
    {
        var keyUserId = $"{RedisPrefixes.ConfirmationToken.Prefix}:{tokenType}:{userId}";
        var tokenValue = await _cacheRepo.GetAsync<string>(keyUserId);

        if (string.IsNullOrEmpty(tokenValue))
            return null;

        var keyTokenValue = $"{RedisPrefixes.ConfirmationToken.Prefix}:{tokenType}:{tokenValue}";
        var tokenDb = await _cacheRepo.GetAsync<RedisConfirmationToken>(keyTokenValue);

        return tokenDb != null && tokenDb.TokenType == tokenType ? tokenDb : null;
    }

    public bool AddToRedis(RedisConfirmationToken token, TimeSpan ttl)
    {
        var keyTokenValue = $"{RedisPrefixes.ConfirmationToken.Prefix}:{token.TokenType}:{token.Value}";
        var isTokenAdded = _cacheRepo.Add(keyTokenValue, token, ttl);

        var keyUserId = $"{RedisPrefixes.ConfirmationToken.Prefix}:{token.TokenType}:{token.UserId}";
        var isTokenUserIdAdded = _cacheRepo.Add(keyUserId, token.Value, ttl);

        return isTokenAdded && isTokenUserIdAdded;
    }

    public async Task<bool> DeleteFromRedisAsync(RedisConfirmationToken token)
    {
        var keyTokenValue = $"{RedisPrefixes.ConfirmationToken.Prefix}:{token.TokenType}:{token.Value}";
        var isTokenRemoved = await _cacheRepo.DeleteAsync(keyTokenValue);

        var keyUserId = $"{RedisPrefixes.ConfirmationToken.Prefix}:{token.TokenType}:{token.UserId}";
        var isTokenUserIdRemoved = await _cacheRepo.DeleteAsync(keyUserId);

        return isTokenRemoved && isTokenUserIdRemoved;
    }
}