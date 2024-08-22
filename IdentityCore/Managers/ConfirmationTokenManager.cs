using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository;
using IdentityCore.Models.Request;

namespace IdentityCore.Managers;

public class ConfirmationTokenManager
{
    #region C-tor and fields

    private readonly ConfirmationTokenRepository _ctRepo;
    private readonly UserRepository _userRepo;

    public ConfirmationTokenManager(ConfirmationTokenRepository ctRepo, UserRepository userRepo)
    {
        _ctRepo = ctRepo;
        _userRepo = userRepo;
    }

    #endregion
    
    public async Task<RedisConfirmationToken> GetTokenAsync(string token, TokenType tokenType) =>
        await _ctRepo.GetFromRedis(token, tokenType);
    
    public async Task<RedisConfirmationToken> GetTokenByUserIdAsync(Guid userId, TokenType tokenType) =>
        await _ctRepo.GetFromByUserIdRedis(userId, tokenType);

    public static string GetNextAttemptTime(RedisConfirmationToken token)
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

    public async Task<RedisConfirmationToken> UpdateCfmToken(
        RedisConfirmationToken token,
        User user,
        RedisUserUpdate userUpdate = null)
    {
        if (token == null || (token.TokenType != TokenType.RegistrationConfirmation && userUpdate == null))
            return null;
        
        var ttl = TokenConfig.GetTtlForTokenType(token.TokenType);
        var isRemovedToken = await _ctRepo.DeleteFromRedisAsync(token);
        var isUpdateTtl = token.TokenType == TokenType.RegistrationConfirmation
            ? await _userRepo.UpdateTtlRegUserAsync(user, ttl)
            : await _userRepo.UpdateTtlUserUpdateAsync(userUpdate, token.TokenType, ttl);

        if (!isRemovedToken || !isUpdateTtl)
            return null;

        if (DateTime.UtcNow - token.Modified >= MailConfig.Values.NextAttemptAvailableAfter)
            token.AttemptCount = 0;

        token.Value = UserHelper.GetToken(token.UserId);
        token.AttemptCount = ++token.AttemptCount;
        token.Modified = DateTime.UtcNow;
        return _ctRepo.AddToRedis(token, ttl) ? token : null;
    }

    public static bool ValidateTokenTypeForRequest(TokenType tokenType, bool isRegistration)
    {
        return (isRegistration && tokenType == TokenType.RegistrationConfirmation)
               || (!isRegistration && tokenType != TokenType.RegistrationConfirmation);
    }
}