using Microsoft.Extensions.Logging;

using IdentityCore.DAL.MariaDb;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository.Base;

namespace IdentityCore.DAL.Repository;

public class UserRepository : DbRepositoryBase<User>
{
    #region Fields and c-tor

    public UserRepository(
        IdentityCoreDbContext dbContext,
        ILogger<UserRepository> logger)
        : base(dbContext, logger)
    { }

    #endregion
}