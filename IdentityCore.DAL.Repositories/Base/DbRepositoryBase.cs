using IdentityCore.DAL.PorstgreSQL;
using IdentityCore.DAL.Models.Base;

namespace IdentityCore.DAL.Repository.Base;

public abstract class DbRepositoryBase<T>
    where T : BaseDbEntity
{
    #region Fields and c-tor

    protected readonly IdentityCoreDbContext DbContext;

    protected DbRepositoryBase(IdentityCoreDbContext dbContext)
    {
        DbContext = dbContext;
    }

    #endregion

    #region CRUD

    public virtual T Create(T entity)
    {
        var entityEntry = DbContext.Add(entity);
        return DbContext.SaveAndCompareAffectedRows()
            ? entityEntry.Entity
            : default;
    }

    public virtual bool Update(T entity)
    {
        DbContext.Update(entity);
        return DbContext.SaveAndCompareAffectedRows();
    }

    public virtual bool Delete(T entity)
    {
        DbContext.Remove(entity);
        return DbContext.SaveAndCompareAffectedRows();
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        var entityEntry = await DbContext.AddAsync(entity);
        return await DbContext.SaveAndCompareAffectedRowsAsync()
            ? entityEntry.Entity
            : null;
    }

    public virtual async Task<bool> UpdateAsync(T entity)
    {
        DbContext.Update(entity);
        return await DbContext.SaveAndCompareAffectedRowsAsync();
    }

    public virtual async Task<bool> DeleteAsync(T entity)
    {
        DbContext.Remove(entity);
        return await DbContext.SaveAndCompareAffectedRowsAsync();
    }

    public async Task<bool> SaveAsync() =>
        await DbContext.SaveAndCompareAffectedRowsAsync();

    #endregion
}