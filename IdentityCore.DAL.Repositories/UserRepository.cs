using Microsoft.EntityFrameworkCore;

using IdentityCore.DAL.MariaDb;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository.Base;

namespace IdentityCore.DAL.Repository;

public class UserRepository : DbRepositoryBase<User>
{
    #region Fields and c-tor

    public UserRepository(IdentityCoreDbContext dbContext) : base(dbContext)
    { }

    #endregion
    
    public async Task<User?> GetUserByIdAsync(Guid id) =>
        await DbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
    
    public async Task<User?> GetUserByUsernameAsync(string username) =>
        await DbContext.Users.FirstOrDefaultAsync(user => user.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    
    public async Task<User?> GetUserByEmailAsync(string email) =>
        await DbContext.Users.FirstOrDefaultAsync(user => user.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

    public async Task<bool> UserExistsByIdAsync(Guid id) =>
        await GetUserByIdAsync(id) is not null;
    
    public async Task<bool> UserExistsByUsernameAsync(string username) =>
        await GetUserByUsernameAsync(username) is not null;
    
    public async Task<bool> UserExistsByEmailAsync(string email) =>
        await GetUserByEmailAsync(email) is not null;
    
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
            return false;
        }
    }
}