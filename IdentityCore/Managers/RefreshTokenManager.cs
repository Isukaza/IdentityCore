using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL.Models;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces;
using IdentityCore.Managers.Interfaces;
using IdentityCore.Models;

namespace IdentityCore.Managers;

public class RefreshTokenManager : IRefreshTokenManager
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    public RefreshTokenManager(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepo = refreshTokenRepository;
    }

    public RefreshToken CreateRefreshToken(User user) =>
        new()
        {
            RefToken = UserHelper.GetToken(user.Id),
            Expires = DateTime.UtcNow.Add(RefToken.Configs.Expires),
            User = user
        };

    public async Task<bool> AddTokenAsync(User user, RefreshToken refreshToken)
    {
        var countTokens = await _refreshTokenRepo.GetCountUserTokensAsync(user.Id);
        if (countTokens >= RefToken.Configs.MaxSessions)
        {
            var tokensToRemove = countTokens - RefToken.Configs.MaxSessions + 1;

            for (var i = 0; i < tokensToRemove; i++)
            {
                await _refreshTokenRepo.DeleteOldestSessionAsync(user.Id);
            }
        }

        return await _refreshTokenRepo.CreateAsync(refreshToken) is not null;
    }

    public async Task<string> UpdateTokenDbAsync(RefreshToken token)
    {
        token.RefToken = UserHelper.GetToken(token.UserId);
        token.Expires = DateTime.UtcNow.Add(RefToken.Configs.Expires);

        return await _refreshTokenRepo.UpdateAsync(token) ? token.RefToken : string.Empty;
    }

    public async Task<OperationResult<RefreshToken>> ValidationRefreshTokenAsync(Guid userId, string token)
    {
        var tokenDb = await _refreshTokenRepo.GetTokenByUserIdAsync(userId, token);
        if (tokenDb is null)
            return new OperationResult<RefreshToken>("Invalid input data");

        if (DateTime.UtcNow < tokenDb.Expires)
            return new OperationResult<RefreshToken>(tokenDb);

        _ = _refreshTokenRepo.DeleteAsync(tokenDb);
        return new OperationResult<RefreshToken>("Token expired");
    }
}