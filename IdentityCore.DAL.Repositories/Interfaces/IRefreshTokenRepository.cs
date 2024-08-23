using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository.Interfaces.Base;

namespace IdentityCore.DAL.Repository.Interfaces;

public interface IRefreshTokenRepository : IDbRepositoryBase<RefreshToken>
{
    Task<int> GetCountUserTokensAsync(Guid id);
    Task<RefreshToken> GetTokenByUserIdAsync(Guid userId, string token);
    Task DeleteOldestSessionAsync(Guid id);
}