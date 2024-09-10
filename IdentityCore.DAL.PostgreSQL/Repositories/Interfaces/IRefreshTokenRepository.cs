using IdentityCore.DAL.PostgreSQL.Models;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.Base;

namespace IdentityCore.DAL.PostgreSQL.Repositories.Interfaces;

public interface IRefreshTokenRepository : IDbRepositoryBase<RefreshToken>
{
    Task<int> GetCountUserTokensAsync(Guid id);
    Task<RefreshToken> GetTokenByUserIdAsync(Guid userId, string token);
    Task DeleteOldestSessionAsync(Guid id);
}