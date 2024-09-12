using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.Base;

namespace IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.db;

public interface IRefreshTokenDbRepository : IDbRepositoryBase<RefreshToken>
{
    Task<int> GetCountUserTokensAsync(Guid id);
    Task<RefreshToken> GetTokenByUserIdAsync(Guid userId, string token);
    Task DeleteOldestSessionAsync(Guid id);
}