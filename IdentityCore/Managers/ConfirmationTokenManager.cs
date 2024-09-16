using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.cache.cachePrefix;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.cache;
using IdentityCore.Managers.Interfaces;
using IdentityCore.Models.Request;

namespace IdentityCore.Managers;

public class ConfirmationTokenManager : IConfirmationTokenManager
{
    #region C-tor and fields

    private readonly IUserCacheRepository _userCacheRepo;
    private readonly ICfmTokenCacheRepository _ctRepo;

    public ConfirmationTokenManager(
        IUserCacheRepository userCacheRepo,
        ICfmTokenCacheRepository ctRepo)
    {
        _userCacheRepo = userCacheRepo;
        _ctRepo = ctRepo;
    }

    #endregion

    public async Task<RedisConfirmationToken> GetTokenAsync(string token, TokenType tokenType) =>
        await _ctRepo.GetFromRedisAsync(token, tokenType);

    public async Task<RedisConfirmationToken> GetTokenByUserIdAsync(Guid userId, TokenType tokenType) =>
        await _ctRepo.GetFromByUserIdRedisAsync(userId, tokenType);

    public string GetNextAttemptTime(RedisConfirmationToken token)
    {
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

    public RedisConfirmationToken CreateConfirmationToken(Guid id, TokenType tokenType)
    {
        var ttl = TokenConfig.GetTtlForTokenType(tokenType);
        var token = new RedisConfirmationToken
        {
            UserId = id,
            Value = UserHelper.GetToken(id),
            TokenType = tokenType
        };

        return _ctRepo.AddToRedis(token, ttl) ? token : null;
    }

    public async Task<RedisConfirmationToken> UpdateCfmTokenAsync(RedisConfirmationToken token)
    {
        if (token == null)
            return null;

        var ttl = TokenConfig.GetTtlForTokenType(token.TokenType);
        var isRemovedToken = await _ctRepo.DeleteFromRedisAsync(token);
        if (!isRemovedToken)
            return null;

        if (DateTime.UtcNow - token.Modified >= MailConfig.Values.NextAttemptAvailableAfter)
            token.AttemptCount = 0;

        token.Value = UserHelper.GetToken(token.UserId);
        token.AttemptCount = ++token.AttemptCount;
        token.Modified = DateTime.UtcNow;
        
        return _ctRepo.AddToRedis(token, ttl) ? token : null;
    }

    public TokenType DetermineConfirmationTokenType(UserUpdateRequest updateRequest)
    {
        if (!string.IsNullOrWhiteSpace(updateRequest.Username))
            return TokenType.UsernameChange;

        if (!string.IsNullOrWhiteSpace(updateRequest.NewPassword))
            return TokenType.PasswordChange;

        return !string.IsNullOrWhiteSpace(updateRequest.Email)
            ? TokenType.EmailChangeOld
            : TokenType.Unknown;
    }
    
    public bool ValidateTokenTypeForRequest(TokenType tokenType, bool isRegistration)
    {
        return (isRegistration && tokenType == TokenType.RegistrationConfirmation)
               || (!isRegistration && tokenType != TokenType.RegistrationConfirmation);
    }
}