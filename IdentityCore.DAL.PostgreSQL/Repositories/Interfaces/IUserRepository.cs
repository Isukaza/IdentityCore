using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.Base;

namespace IdentityCore.DAL.PostgreSQL.Repositories.Interfaces;

public interface IUserRepository : IDbRepositoryBase<User>
{
    Task<User> GetUserByIdAsync(Guid id);
    Task<User> GetUserByUsernameAsync(string username);
    Task<User> GetUserByEmailAsync(string email);
}