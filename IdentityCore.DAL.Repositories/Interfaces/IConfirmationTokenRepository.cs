using IdentityCore.DAL.Models;
using IdentityCore.DAL.Models.enums;

namespace IdentityCore.DAL.Repository.Interfaces;

public interface IConfirmationTokenRepository
{
    Task<RedisConfirmationToken> GetFromRedisAsync(string key, TokenType tokenType);
    Task<RedisConfirmationToken> GetFromByUserIdRedisAsync(Guid userId, TokenType tokenType);
    bool AddToRedis(RedisConfirmationToken token, TimeSpan ttl);
    Task<bool> DeleteFromRedisAsync(RedisConfirmationToken token);
}