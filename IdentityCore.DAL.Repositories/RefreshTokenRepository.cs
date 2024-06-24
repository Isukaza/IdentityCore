using IdentityCore.DAL.MariaDb;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository.Base;
using Microsoft.EntityFrameworkCore;

namespace IdentityCore.DAL.Repository;

public class RefreshTokenRepository : DbRepositoryBase<RefreshToken>
{
    #region Fields and c-tor

    public RefreshTokenRepository(IdentityCoreDbContext dbContext) : base(dbContext)
    { }

    #endregion

    public IEnumerable<RefreshToken> GetUserTokens(Guid id) =>
         DbContext.RefreshTokens.Where(rt => rt.UserId == id);

    public async Task<int> GetCountUserTokens(Guid id) =>
        await DbContext.RefreshTokens.CountAsync(rt => rt.UserId == id);

    public async Task<bool> DeleteOldestSession(Guid id)
    {
        var oldestRToken = GetUserTokens(id).MinBy(rt => rt.Created);
        return await base.DeleteAsync(oldestRToken);
    }
}