using System.Text;
using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository;

namespace IdentityCore.Managers;

public class ConfirmationRegistrationManager
{
    #region C-tor and fields

    private const int Sha512ByteSize = 64;

    private readonly UserRepository _userRepo;
    private readonly ConfirmationRegistrationRepository _crRepo;

    public ConfirmationRegistrationManager(UserRepository userRepo,
        ConfirmationRegistrationRepository crRepo)
    {
        _userRepo = userRepo;
        _crRepo = crRepo;
    }

    #endregion

    public static string GetNextAttemptAvailabilityTime(RegistrationToken token)
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

    public static bool IsTokenValid(string token)
    {
        var bufferSize = (int)Math.Ceiling(token.Length * 3.0 / 4.0);
        var buffer = new Span<byte>(new byte[bufferSize]);
        if (Convert.TryFromBase64String(token, buffer, out var bytes))
            return bytes == Sha512ByteSize;

        return false;
    }

    public static RegistrationToken CreateConfirmationRegistrationToken(User user) =>
        new()
        {
            RegToken = UserHelper.GetConfirmationRegistrationToken(user.Id),
            Expires = DateTime.UtcNow.Add(TimeSpan.FromHours(1)),
            AttemptCount = 1
        };

    public async Task<string> ActivatedUser(string token)
    {
        var tokenDb = await _crRepo.GetRegistrationTokenByTokenAsync(token);
        if (tokenDb is null)
            return "Invalid token";

        tokenDb.User.IsActive = true;

        if (!await _userRepo.UpdateAsync(tokenDb.User))
            return "Invalid token";

        await _crRepo.DeleteAsync(tokenDb);

        return string.Empty;
    }

    public async Task<RegistrationToken> UpdateRegistrationToken(RegistrationToken token, Guid userId)
    {
        if (token == null)
            return null;

        token.RegToken = UserHelper.GetConfirmationRegistrationToken(userId);

        if (DateTime.UtcNow - token.Modified >= Mail.Configs.NextAttemptAvailableAfter)
            token.AttemptCount = 0;

        token.AttemptCount = ++token.AttemptCount;

        return await _crRepo.UpdateAsync(token) ? token : null;
    }

    public async Task DeleteExpiredTokens()
    {
        var expiredTokens = _crRepo.GetExpiredTokens();
        await _crRepo.DeleteRange(expiredTokens);
    }
}