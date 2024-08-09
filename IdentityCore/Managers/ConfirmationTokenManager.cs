using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository;
using IdentityCore.Models;
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

    public async Task<ConfirmationToken> GetTokenAsync(string token, TokenType tokenType) =>
        await _ctRepo.GetFromRedis(token, tokenType);

    public static string GetNextAttemptTime(ConfirmationToken token)
    {
        var timeDifference = DateTime.UtcNow - token.Modified;
        if ((token.AttemptCount < Mail.Configs.MaxAttemptsConfirmationResend
             || timeDifference > Mail.Configs.NextAttemptAvailableAfter)
            && timeDifference > Mail.Configs.MinIntervalBetweenAttempts)
            return string.Empty;

        var nextAvailableTime = token.Modified.Add(token.AttemptCount < Mail.Configs.MaxAttemptsConfirmationResend
            ? Mail.Configs.MinIntervalBetweenAttempts
            : Mail.Configs.NextAttemptAvailableAfter);
        return nextAvailableTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    public ConfirmationToken CreateConfirmationToken(User user, TokenType tokenType)
    {
        var ttl = TokenConfig.GetTtlForTokenType(tokenType);
        var token = new ConfirmationToken
        {
            UserId = user.Id,
            Value = UserHelper.GetToken(user.Id),
            TokenType = tokenType
        };
        
        return _ctRepo.AddToRedis(token, ttl) ? token : null;
    }

    public async Task<ConfirmationToken> UpdateRegistrationToken(ConfirmationToken token, Guid userId)
    {
        if (token == null)
            return null;

        var ttl = await _ctRepo.GetFromRedisTllAsync(token.Value);
        if (!await _ctRepo.DeleteFromRedisAsync(token))
            return null;
        
        token.Value = UserHelper.GetToken(userId);
        if (DateTime.UtcNow - token.Modified >= Mail.Configs.NextAttemptAvailableAfter)
            token.AttemptCount = 0;

        token.AttemptCount = ++token.AttemptCount;
        token.Modified = DateTime.UtcNow;
        return _ctRepo.AddToRedis(token, ttl) ? token : null;
    }

    public async Task<bool> DeleteToken(ConfirmationToken token) =>
        await _ctRepo.DeleteFromRedisAsync(token);
    
    public async Task<OperationResult<ConfirmationToken>> ValidateResendConfirmationRegistrationMail(
        ResendConfirmationEmailRequest emailRequest)
    {
        if (string.IsNullOrWhiteSpace(emailRequest.Email))
            return new OperationResult<ConfirmationToken>("Invalid input data");

        var token = await _ctRepo.GetFromRedis(emailRequest.UserId, TokenType.RegistrationConfirmation);
        if (token is null)
            return new OperationResult<ConfirmationToken>("Invalid input data");
        
        var user = await _userRepo.GetUserFromRedisByIdAsync(emailRequest.UserId);
        return user == null || user.Email != emailRequest.Email
            ? new OperationResult<ConfirmationToken>("Invalid input data")
            : new OperationResult<ConfirmationToken>(token);
    }
}