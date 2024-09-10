using IdentityCore.DAL.PostgreSQL.Models;
using IdentityCore.DAL.PostgreSQL.Models.enums;

namespace IdentityCore.DAL.PostgreSQL.Repositories.Interfaces;

public interface IConfirmationTokenRepository
{
    Task<RedisConfirmationToken> GetFromRedisAsync(string key, TokenType tokenType);
    Task<RedisConfirmationToken> GetFromByUserIdRedisAsync(Guid userId, TokenType tokenType);
    bool AddToRedis(RedisConfirmationToken token, TimeSpan ttl);
    Task<bool> DeleteFromRedisAsync(RedisConfirmationToken token);
}