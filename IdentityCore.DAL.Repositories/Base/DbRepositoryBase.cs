using Microsoft.Extensions.Logging;

using IdentityCore.DAL.MariaDb;
using IdentityCore.DAL.Models.Base;

namespace IdentityCore.DAL.Repository.Base;

public abstract class DbRepositoryBase<T>
    where T : BaseDbEntity
{
    #region Fields and c-tor

    protected readonly ILogger<DbRepositoryBase<T>> Logger;
    protected readonly IdentityCoreDbContext DbContext;

    protected DbRepositoryBase(
        IdentityCoreDbContext dbContext,
        ILogger<DbRepositoryBase<T>> logger)
    {
        DbContext = dbContext;
        Logger = logger;
    }

    #endregion
}