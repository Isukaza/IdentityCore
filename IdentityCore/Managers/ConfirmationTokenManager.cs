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

    public async Task<RedisConfirmationToken> GetTokenAsync(string token, TokenType tokenType) =>
        await _ctRepo.GetFromRedis(token, tokenType);

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

    public async Task<RedisConfirmationToken> UpdateCfmToken(RedisConfirmationToken token, Guid userId)
    {
        if (token == null)
            return null;

        var ttl = await _ctRepo.GetFromRedisTllAsync(token.Value, token.TokenType);
        if (!await _ctRepo.DeleteFromRedisAsync(token))
            return null;

        token.Value = UserHelper.GetToken(userId);
        if (DateTime.UtcNow - token.Modified >= MailConfig.Values.NextAttemptAvailableAfter)
            token.AttemptCount = 0;

        token.AttemptCount = ++token.AttemptCount;
        token.Modified = DateTime.UtcNow;
        return _ctRepo.AddToRedis(token, ttl) ? token : null;
    }

    public async Task<bool> DeleteToken(RedisConfirmationToken token) =>
        await _ctRepo.DeleteFromRedisAsync(token);

    public async Task<OperationResult<RedisConfirmationToken>> ValidateResendCfmTokenMail(
        ResendConfirmationEmailRequest emailRequest)
    {
        if (string.IsNullOrWhiteSpace(emailRequest.Email))
            return new OperationResult<RedisConfirmationToken>("Invalid input data");

        var token = await _ctRepo.GetFromRedis(emailRequest.UserId, TokenType.RegistrationConfirmation);
        if (token is null)
            return new OperationResult<RedisConfirmationToken>("Invalid input data");

        var user = await _userRepo
            .GetRegUserFromRedisByIdAsync(emailRequest.UserId);
        return user == null || user.Email != emailRequest.Email
            ? new OperationResult<RedisConfirmationToken>("Invalid input data")
            : new OperationResult<RedisConfirmationToken>(token);
    }
}