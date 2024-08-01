using IdentityCore.DAL.PorstgreSQL;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository.Base;
using Microsoft.EntityFrameworkCore;

namespace IdentityCore.DAL.Repository;

public class RefreshTokenRepository : DbRepositoryBase<RefreshToken>
{
    #region C-tor

    public RefreshTokenRepository(IdentityCoreDbContext dbContext) : base(dbContext)
    { }

    #endregion

    public async Task<int> GetCountUserTokens(Guid id) =>
        await DbContext.RefreshTokens.CountAsync(rt => rt.UserId == id);
    
    private IQueryable<RefreshToken> GetUserTokens(Guid id) =>
        DbContext.RefreshTokens.Where(rt => rt.UserId == id);
    
    public async Task<RefreshToken> GetTokenByUserIdAsync(Guid userId, string token) =>
        await DbContext.RefreshTokens
            .Include(u => u.User)
            .FirstOrDefaultAsync(refT => refT.UserId == userId && refT.RefToken == token);

    public async Task DeleteOldestSession(Guid id)
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