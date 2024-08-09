using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository.Base;

namespace IdentityCore.DAL.Repository;

public class ConfirmationTokenRepository
{
    #region C-tor

    private const string RedisKeyPrefix = "CT";
    private readonly CacheRepositoryBase _cacheRepo;

    public ConfirmationTokenRepository(CacheRepositoryBase cacheRepo)
    {
        _cacheRepo = cacheRepo;
    }

    #endregion

    public bool AddToRedis(ConfirmationToken token, TimeSpan ttl)
    {
        var keyTokenValue = $"{RedisKeyPrefix}:{token.Value}";
        var isTokenAdded = _cacheRepo.Add(keyTokenValue, token, ttl);

        var keyUserId = $"{RedisKeyPrefix}:{token.UserId}";
        var isTokenUserIdAdded = _cacheRepo.Add(keyUserId, token.Value, ttl);

        return isTokenAdded && isTokenUserIdAdded;
    }

    public async Task<ConfirmationToken> GetFromRedis(string key, TokenType tokenType)
    {
        var keyRedis = $"{RedisKeyPrefix}:{key}";
        var tokenDb = await _cacheRepo.GetAsync<ConfirmationToken>(keyRedis);
        return tokenDb != null && tokenDb.TokenType == tokenType ? tokenDb : null;
    }
    
    public async Task<ConfirmationToken> GetFromRedis(Guid userId, TokenType tokenType)
    {
        var keyUserId = $"{RedisKeyPrefix}:{userId}";
        var tokenValue = await _cacheRepo.GetAsync<string>(keyUserId);

        var keyTokenValue = $"{RedisKeyPrefix}:{tokenValue}";
        var tokenDb = await _cacheRepo.GetAsync<ConfirmationToken>(keyTokenValue);
        
        return tokenDb != null && tokenDb.TokenType == tokenType ? tokenDb : null;
    }

    public async Task<TimeSpan> GetFromRedisTllAsync(string token)
    {
        var key = $"{RedisKeyPrefix}:{token}";
        return await _cacheRepo.GetTtlAsync(key) ?? TimeSpan.Zero;
    }

    public async Task<bool> DeleteFromRedisAsync(ConfirmationToken token)
    {
        var keyTokenValue = $"{RedisKeyPrefix}:{token.Value}";
        var isTokenRemoved = await _cacheRepo.DeleteAsync(keyTokenValue);

        var keyUserId = $"{RedisKeyPrefix}:{token.UserId}";
        var isTokenUserIdRemoved = await _cacheRepo.DeleteAsync(keyUserId);

        return isTokenRemoved && isTokenUserIdRemoved;
    }
}