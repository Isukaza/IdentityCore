using IdentityCore.DAL.PostgreSQL.Models.enums;

namespace IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.cache;

public interface ICfmTokenCacheRepository
{
    Task<T> GetTokenByTokenType<T>(string key, TokenType tokenType);
    bool Add<T>(string key, T value, TokenType tokenType, TimeSpan ttl);
    Task<bool> DeleteAsync(string key, TokenType tokenType);
}