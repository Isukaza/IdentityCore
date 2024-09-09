using IdentityCore.DAL.Models;
using IdentityCore.DAL.Models.enums;

namespace IdentityCore.Managers.Interfaces;

public interface IConfirmationTokenManager
{
    Task<RedisConfirmationToken> GetTokenAsync(string token, TokenType tokenType);
    Task<RedisConfirmationToken> GetTokenByUserIdAsync(Guid userId, TokenType tokenType);
    string GetNextAttemptTime(RedisConfirmationToken token);

    RedisConfirmationToken CreateConfirmationToken(Guid id, TokenType tokenType);

    Task<RedisConfirmationToken> UpdateCfmTokenAsync(
        RedisConfirmationToken token,
        User user,
        RedisUserUpdate userUpdate = null);

    bool ValidateTokenTypeForRequest(TokenType tokenType, bool isRegistration);
}