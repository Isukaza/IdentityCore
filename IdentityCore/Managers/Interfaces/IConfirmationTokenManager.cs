using IdentityCore.DAL.PostgreSQL.Models;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.Models.Request;

namespace IdentityCore.Managers.Interfaces;

public interface IConfirmationTokenManager
{
    Task<RedisConfirmationToken> GetTokenAsync(string token, TokenType tokenType);
    Task<RedisConfirmationToken> GetTokenByUserIdAsync(Guid userId, TokenType tokenType);
    string GetNextAttemptTime(RedisConfirmationToken token);
    TokenType DetermineConfirmationTokenType(UserUpdateRequest updateRequest);

    RedisConfirmationToken CreateConfirmationToken(Guid id, TokenType tokenType);

    Task<RedisConfirmationToken> UpdateCfmTokenAsync(
        RedisConfirmationToken token,
        User user,
        RedisUserUpdate userUpdate = null);

    bool ValidateTokenTypeForRequest(TokenType tokenType, bool isRegistration);
}