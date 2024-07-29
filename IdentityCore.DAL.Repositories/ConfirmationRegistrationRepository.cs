using IdentityCore.DAL.MariaDb;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository.Base;
using Microsoft.EntityFrameworkCore;

namespace IdentityCore.DAL.Repository;

public class ConfirmationRegistrationRepository : DbRepositoryBase<RegistrationToken>
{
    #region C-tor

    public ConfirmationRegistrationRepository(IdentityCoreDbContext dbContext) : base(dbContext)
    { }

    #endregion
    
    public async Task<RegistrationToken> GetRegistrationTokenByTokenAsync(string token) =>
        await DbContext.RegistrationTokens
            .Include(u=> u.User)
            .FirstOrDefaultAsync(u => u.RegToken == token);

    public IQueryable<RegistrationToken> GetExpiredTokens() =>
        DbContext.RegistrationTokens.Where(t => DateTime.UtcNow > t.Expires);

    public async Task DeleteRange(IQueryable<RegistrationToken> expiredTokens)
    {
        DbContext.RemoveRange(expiredTokens);
        await DbContext.SaveAndCompareAffectedRowsAsync();
    }
}