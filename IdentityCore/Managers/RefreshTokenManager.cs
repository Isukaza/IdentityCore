using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository;
using IdentityCore.Models;
using IdentityCore.Models.Response;

namespace IdentityCore.Managers;

public class RefreshTokenManager
{
    private readonly RefreshTokenRepository _refreshTokenRepo;

    public RefreshTokenManager(RefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepo = refreshTokenRepository;
    }

    public static RefreshToken CreateRefreshToken(User user) =>
        new()
        {
            RefToken = UserHelper.GetToken(user.Id),
            Expires = DateTime.UtcNow.Add(RefToken.Configs.Expires),
            User = user
        };

    public async Task<bool> AddToken(User user, RefreshToken refreshToken)
    {
        var countTokens = await _refreshTokenRepo.GetCountUserTokens(user.Id);
        if (countTokens >= RefToken.Configs.MaxSessions)
        {
            var tokensToRemove = countTokens - RefToken.Configs.MaxSessions + 1;

            for (var i = 0; i < tokensToRemove; i++)
            {
                await _refreshTokenRepo.DeleteOldestSession(user.Id);
            }
        }

        return await _refreshTokenRepo.CreateAsync(refreshToken) is not null;
    }
    
    public async Task<string> UpdateTokenDb(RefreshToken token)
    {
        token.RefToken = UserHelper.GetToken(token.UserId);
        token.Expires = DateTime.UtcNow.Add(RefToken.Configs.Expires);

        return await _refreshTokenRepo.UpdateAsync(token) ? token.RefToken : string.Empty;
    }
    
    public async Task<OperationResult<RefreshToken>> ValidationRefreshToken(Guid userId, string token)
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