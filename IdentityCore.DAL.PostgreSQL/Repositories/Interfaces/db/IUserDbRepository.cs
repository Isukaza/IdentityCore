using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.Base;

namespace IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.db;

public interface IUserDbRepository : IDbRepositoryBase<User>
{
    Task<User> GetUserByIdAsync(Guid id);
    Task<User> GetUserByUsernameAsync(string username);
    Task<User> GetUserByEmailAsync(string email);
}