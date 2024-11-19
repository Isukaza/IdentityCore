using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.db;
using IdentityCore.Managers.Interfaces;
using IdentityCore.Models;

namespace IdentityCore.Managers;

public class RefreshTokenManager : IRefreshTokenManager
{
    private readonly IRefreshTokenDbRepository _refreshTokenDbRepo;

    public RefreshTokenManager(IRefreshTokenDbRepository refreshTokenDbRepository)
    {
        _refreshTokenDbRepo = refreshTokenDbRepository;
    }

    public RefreshToken CreateRefreshToken(User user)
    {
        if (user is null)
            return null;

        return new RefreshToken
        {
            RefToken = UserHelper.GetToken(user.Id),
            Expires = DateTime.UtcNow.Add(RefTokenConfig.Values.Expires),
            User = user
        };
    }

    public async Task<bool> AddTokenAsync(User user, RefreshToken refreshToken)
    {
        if (user is null || refreshToken is null)
            return false;

        var countTokens = await _refreshTokenDbRepo.GetCountUserTokensAsync(user.Id);
        if (countTokens >= RefTokenConfig.Values.MaxSessions)
        {
            var tokensToRemove = countTokens - RefTokenConfig.Values.MaxSessions + 1;
            for (var i = 0; i < tokensToRemove; i++)
            {
                await _refreshTokenDbRepo.DeleteOldestSessionAsync(user.Id);
            }
        }

        return await _refreshTokenDbRepo.CreateAsync(refreshToken) is not null;
    }

    public async Task<string> UpdateTokenDbAsync(RefreshToken token)
    {
        if (token is null)
            return string.Empty;

        token.RefToken = UserHelper.GetToken(token.UserId);
        token.Expires = DateTime.UtcNow.Add(RefTokenConfig.Values.Expires);

        return await _refreshTokenDbRepo.UpdateAsync(token) ? token.RefToken : string.Empty;
    }

    public async Task<OperationResult<RefreshToken>> ValidationRefreshTokenAsync(Guid userId, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new OperationResult<RefreshToken>("Invalid input data");

        var tokenDb = await _refreshTokenDbRepo.GetTokenByUserIdAsync(userId, token);
        if (tokenDb is null)
            return new OperationResult<RefreshToken>("Invalid input data");

        if (DateTime.UtcNow < tokenDb.Expires)
            return new OperationResult<RefreshToken>(tokenDb);

        _ = _refreshTokenDbRepo.DeleteAsync(tokenDb);
        return new OperationResult<RefreshToken>("Token expired");
    }
}