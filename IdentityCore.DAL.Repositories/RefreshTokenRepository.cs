using IdentityCore.DAL.MariaDb;
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
    
    private IQueryable<RefreshToken> GetUserTokens(Guid id) =>
         DbContext.RefreshTokens.Where(rt => rt.UserId == id);

    public async Task<int> GetCountUserTokens(Guid id) =>
        await DbContext.RefreshTokens.CountAsync(rt => rt.UserId == id);

    public async Task<bool> DeleteOldestSession(Guid id)
    {
        var rTokens = GetUserTokens(id);

        if (!rTokens.Any())
            return true;

        var oldestRToken = rTokens
            .ToList()
            .MinBy(rt => rt.Created);

        return await base.DeleteAsync(oldestRToken!);
    }
}