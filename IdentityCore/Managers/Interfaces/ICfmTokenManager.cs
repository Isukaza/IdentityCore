using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.Models.Interface;
using IdentityCore.Models.Request;

namespace IdentityCore.Managers.Interfaces;

public interface ICfmTokenManager
{
    Task<RedisConfirmationToken> GetTokenAsync(string token, TokenType tokenType);
    Task<RedisConfirmationToken> GetTokenByUserIdAsync(Guid userId, TokenType tokenType);
    string GetNextAttemptTime(RedisConfirmationToken token);

    RedisConfirmationToken CreateToken(Guid id, TokenType tokenType);

    Task<RedisConfirmationToken> UpdateTokenAsync(RedisConfirmationToken token);

    Task<bool> DeleteTokenAsync(RedisConfirmationToken token);

    TokenType DetermineTokenType(IUserUpdate updateRequest);
    bool ValidateTokenTypeForRequest(TokenType tokenType, bool isRegistrationProcess);
}