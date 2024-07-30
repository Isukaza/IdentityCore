using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository;

namespace IdentityCore.Managers;

public class RefreshTokenManager
{
    private readonly RefreshTokenRepository _refreshTokenRepo;

    public RefreshTokenManager(RefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepo = refreshTokenRepository;
    }

    public async Task<string> RefreshToken(RefreshToken token)
    {
        token.RefToken = UserHelper.GenerateRefreshToken();
        token.Expires = DateTime.UtcNow.Add(Rt.Configs.Expires);

        return await _refreshTokenRepo.UpdateAsync(token) ? token.RefToken : string.Empty;
    }

    public async Task<bool> AddToken(User user, RefreshToken refreshToken)
    {
        var countTokens = await _refreshTokenRepo.GetCountUserTokens(user.Id);

        if (countTokens >= Rt.Configs.MaxSessions)
        {
            var tokensToRemove = countTokens - Rt.Configs.MaxSessions + 1;

            for (var i = 0; i < tokensToRemove; i++)
            {
                _ = await _refreshTokenRepo.DeleteOldestSession(user.Id);
            }
        }

        return await _refreshTokenRepo.CreateAsync(refreshToken) is not null;
    }
}