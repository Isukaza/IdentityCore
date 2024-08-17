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

    public async Task<RedisConfirmationToken> GetFromRedis(string key, TokenType tokenType)
    {
        var keyRedis = $"{RedisKeyPrefix}:{tokenType}:{key}";
        var tokenDb = await _cacheRepo.GetAsync<RedisConfirmationToken>(keyRedis);
        return tokenDb != null && tokenDb.TokenType == tokenType ? tokenDb : null;
    }
    
    public async Task<RedisConfirmationToken> GetFromByUserIdRedis(Guid userId, TokenType tokenType)
    {
        var keyUserId = $"{RedisKeyPrefix}:{tokenType}:{userId}";
        var tokenValue = await _cacheRepo.GetAsync<string>(keyUserId);

        if (string.IsNullOrEmpty(tokenValue))
            return null;
        
        var keyTokenValue = $"{RedisKeyPrefix}:{tokenType}:{tokenValue}";
        var tokenDb = await _cacheRepo.GetAsync<RedisConfirmationToken>(keyTokenValue);
        
        return tokenDb != null && tokenDb.TokenType == tokenType ? tokenDb : null;
    }
    
    public bool AddToRedis(RedisConfirmationToken token, TimeSpan ttl)
    {
        var keyTokenValue = $"{RedisKeyPrefix}:{token.TokenType}:{token.Value}";
        var isTokenAdded = _cacheRepo.Add(keyTokenValue, token, ttl);

        var keyUserId = $"{RedisKeyPrefix}:{token.TokenType}:{token.UserId}";
        var isTokenUserIdAdded = _cacheRepo.Add(keyUserId, token.Value, ttl);

        return isTokenAdded && isTokenUserIdAdded;
    }

    public async Task<bool> DeleteFromRedisAsync(RedisConfirmationToken token)
    {
        var keyTokenValue = $"{RedisKeyPrefix}:{token.TokenType}:{token.Value}";
        var isTokenRemoved = await _cacheRepo.DeleteAsync(keyTokenValue);

        var keyUserId = $"{RedisKeyPrefix}:{token.TokenType}:{token.UserId}";
        var isTokenUserIdRemoved = await _cacheRepo.DeleteAsync(keyUserId);

        return isTokenRemoved && isTokenUserIdRemoved;
    }
}