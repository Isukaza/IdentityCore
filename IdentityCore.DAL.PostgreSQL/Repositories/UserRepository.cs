using Microsoft.EntityFrameworkCore;

using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Repositories.Base;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces;

namespace IdentityCore.DAL.PostgreSQL.Repositories;

public class UserRepository : DbRepositoryBase<User>, IUserRepository
{
    #region C-tor
    public UserRepository(IdentityCoreDbContext dbContext) : base(dbContext)
    { }

    #endregion
    
    public async Task<User> GetUserByIdAsync(Guid id) =>
        await DbContext.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User> GetUserByUsernameAsync(string username)
    {
        if (DbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return await DbContext.Users
                .FirstOrDefaultAsync(u => u.Username.Equals(username, StringComparison.CurrentCultureIgnoreCase));

        return await DbContext.Users
            .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Username, username));
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
        if (DbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return await DbContext.Users
                .FirstOrDefaultAsync(u => u.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase));

        return await DbContext.Users
            .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email, email));
    }
}