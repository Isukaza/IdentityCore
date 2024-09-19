using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.cache;
using IdentityCore.Managers.Interfaces;
using IdentityCore.Models.Request;

namespace IdentityCore.Managers;

public class CfmTokenManager : ICfmTokenManager
{
    #region C-tor and fields

    private readonly ICfmTokenCacheRepository _ctCacheRepo;

    public CfmTokenManager(ICfmTokenCacheRepository ctCacheRepo)
    {
        _ctCacheRepo = ctCacheRepo;
    }

    #endregion

    public async Task<RedisConfirmationToken> GetTokenAsync(string token, TokenType tokenType)
    {
        if (!DataHelper.IsTokenValid(token) || tokenType == TokenType.Unknown)
            return null;

        var tokenDb = await _ctCacheRepo.GetTokenByTokenType<RedisConfirmationToken>(token, tokenType);
        return tokenDb != null && tokenDb.TokenType == tokenType ? tokenDb : null;
    }

    public async Task<RedisConfirmationToken> GetTokenByUserIdAsync(Guid userId, TokenType tokenType)
    {
        var tokenValue = await _ctCacheRepo.GetTokenByTokenType<string>(userId.ToString(), tokenType);
        if (string.IsNullOrEmpty(tokenValue))
            return null;

        var tokenDb = await _ctCacheRepo.GetTokenByTokenType<RedisConfirmationToken>(tokenValue, tokenType);
        return tokenDb != null && tokenDb.TokenType == tokenType ? tokenDb : null;
    }

    public string GetNextAttemptTime(RedisConfirmationToken token)
    {
        if (token == null)
            return "Invalid token";

        var timeDifference = DateTime.UtcNow - token.Modified;
        if ((token.AttemptCount < MailConfig.Values.MaxAttemptsConfirmationResend
             || timeDifference > MailConfig.Values.NextAttemptAvailableAfter)
            && timeDifference > MailConfig.Values.MinIntervalBetweenAttempts)
            return string.Empty;

        var nextAvailableTime = token.Modified.Add(token.AttemptCount < MailConfig.Values.MaxAttemptsConfirmationResend
            ? MailConfig.Values.MinIntervalBetweenAttempts
            : MailConfig.Values.NextAttemptAvailableAfter);
        return nextAvailableTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    public async Task<bool> DeleteTokenAsync(RedisConfirmationToken token)
    {
        if (token == null)
            return false;

        var isTokenRemoved = await _ctCacheRepo.DeleteAsync(token.Value, token.TokenType);
        var isTokenUserIdRemoved = await _ctCacheRepo.DeleteAsync(token.UserId.ToString(), token.TokenType);

        return isTokenRemoved && isTokenUserIdRemoved;
    }

    public RedisConfirmationToken CreateToken(Guid id, TokenType tokenType)
    {
        if (tokenType == TokenType.Unknown)
            return null;
        
        var ttl = TokenConfig.GetTtlForTokenType(tokenType);
        var token = new RedisConfirmationToken
        {
            UserId = id,
            Value = UserHelper.GetToken(id),
            TokenType = tokenType
        };

        return AddToken(token, ttl) ? token : null;
    }

    public async Task<RedisConfirmationToken> UpdateTokenAsync(RedisConfirmationToken token)
    {
        if (token == null)
            return null;

        var ttl = TokenConfig.GetTtlForTokenType(token.TokenType);
        var isRemovedToken = await DeleteTokenAsync(token);
        if (!isRemovedToken)
            return null;

        if (DateTime.UtcNow - token.Modified >= MailConfig.Values.NextAttemptAvailableAfter)
            token.AttemptCount = 0;

        token.Value = UserHelper.GetToken(token.UserId);
        token.AttemptCount = ++token.AttemptCount;
        token.Modified = DateTime.UtcNow;

        return AddToken(token, ttl) ? token : null;
    }

    public TokenType DetermineTokenType(UserUpdateRequest updateRequest)
    {
        if (updateRequest == null)
            return TokenType.Unknown;

        if (!string.IsNullOrWhiteSpace(updateRequest.Username))
            return TokenType.UsernameChange;

        if (!string.IsNullOrWhiteSpace(updateRequest.NewPassword))
            return TokenType.PasswordChange;

        return !string.IsNullOrWhiteSpace(updateRequest.Email)
            ? TokenType.EmailChangeOld
            : TokenType.Unknown;
    }

    public bool ValidateTokenTypeForRequest(TokenType tokenType, bool isRegistrationProcess)
    {
        if (isRegistrationProcess)
            return tokenType == TokenType.RegistrationConfirmation;

        return tokenType != TokenType.RegistrationConfirmation && tokenType != TokenType.Unknown;
    }

    private bool AddToken(RedisConfirmationToken token, TimeSpan ttl)
    {
        var isTokenAdded = _ctCacheRepo.Add(token.Value, token, token.TokenType, ttl);
        var isTokenUserIdAdded = _ctCacheRepo.Add(token.UserId.ToString(), token.Value, token.TokenType, ttl);

        return isTokenAdded && isTokenUserIdAdded;
    }
}