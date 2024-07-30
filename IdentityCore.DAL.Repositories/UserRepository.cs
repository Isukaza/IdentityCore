using Microsoft.EntityFrameworkCore;

using IdentityCore.DAL.MariaDb;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository.Base;

namespace IdentityCore.DAL.Repository;

public class UserRepository : DbRepositoryBase<User>
{
    #region C-tor

    public UserRepository(IdentityCoreDbContext dbContext) : base(dbContext)
    { }

    #endregion

    #region Get

    public async Task<User> GetUserByIdAsync(Guid id) =>
        await DbContext.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User> GetUserByUsernameAsync(string username) =>
        await DbContext.Users
            .FirstOrDefaultAsync(user => EF.Functions.ILike(user.Username, username));

    public async Task<User> GetUserByEmailAsync(string email) =>
        await DbContext.Users
            .FirstOrDefaultAsync(user => EF.Functions.ILike(user.Email, email));

    public async Task<User> GetUserWithTokensByUsernameAsync(string username) =>
        await DbContext.Users
            .Include(rt => rt.RefreshTokens)
            .FirstOrDefaultAsync(user => EF.Functions.ILike(user.Username, username));

    #endregion

    #region Check

    public async Task<bool> UserExistsByUsernameAsync(string username) =>
        await GetUserByUsernameAsync(username) is not null;

    public async Task<bool> UserExistsByEmailAsync(string email) =>
        await GetUserByEmailAsync(email) is not null;

    #endregion

    public async Task<bool> AddedRange(IEnumerable<User> users)
    {
        await using var transaction = await DbContext.Database.BeginTransactionAsync();
        try
        {
            await DbContext.Users.AddRangeAsync(users);
            var result = await SaveAsync();

            await transaction.CommitAsync();

            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }
}