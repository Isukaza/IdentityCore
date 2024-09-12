using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Repositories.Base;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.db;
using Microsoft.EntityFrameworkCore;

namespace IdentityCore.DAL.PostgreSQL.Repositories.db;

public class RefreshTokenDbRepository : DbRepositoryBase<RefreshToken>, IRefreshTokenDbRepository
{
    #region C-tor

    public RefreshTokenDbRepository(IdentityCoreDbContext dbContext) : base(dbContext)
    { }

    #endregion

    public async Task<int> GetCountUserTokensAsync(Guid id) =>
        await DbContext.RefreshTokens.CountAsync(rt => rt.UserId == id);

    public async Task<RefreshToken> GetTokenByUserIdAsync(Guid userId, string token) =>
        await DbContext.RefreshTokens
            .Include(u => u.User)
            .FirstOrDefaultAsync(refT => refT.UserId == userId && refT.RefToken == token);

    private IQueryable<RefreshToken> GetUserTokens(Guid id) =>
        DbContext.RefreshTokens.Where(rt => rt.UserId == id);

    public async Task DeleteOldestSessionAsync(Guid id)
    {
        var rTokens = GetUserTokens(id);

        if (!rTokens.Any())
            return;

        var oldestRToken = rTokens
            .ToList()
            .MinBy(rt => rt.Created);

        _ = await base.DeleteAsync(oldestRToken!);
    }
}