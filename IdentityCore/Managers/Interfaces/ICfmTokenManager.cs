using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.Models.Request;

namespace IdentityCore.Managers.Interfaces;

public interface ICfmTokenManager
{
    Task<RedisConfirmationToken> GetTokenAsync(string token, TokenType tokenType);
    Task<RedisConfirmationToken> GetTokenByUserIdAsync(Guid userId, TokenType tokenType);
    string GetNextAttemptTime(RedisConfirmationToken token);

    bool AddToken(RedisConfirmationToken token, TimeSpan ttl);
    RedisConfirmationToken CreateConfirmationToken(Guid id, TokenType tokenType);

    Task<RedisConfirmationToken> UpdateCfmTokenAsync(RedisConfirmationToken token);

    Task<bool> DeleteTokenAsync(RedisConfirmationToken token);

    TokenType DetermineConfirmationTokenType(UserUpdateRequest updateRequest);
    bool ValidateTokenTypeForRequest(TokenType tokenType, bool isRegistration);
}