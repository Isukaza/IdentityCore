using Microsoft.EntityFrameworkCore;

using IdentityCore.DAL.PorstgreSQL;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository.Base;

namespace IdentityCore.DAL.Repository;

public class UserRepository : DbRepositoryBase<User>
{
    #region C-tor and Fields

    private const string RedisKeyPrefixNewUser = "UR";
    private const string RedisKeyPrefixNewUserName = "UR:Username";
    private const string RedisKeyPrefixNewUserEmail = "UR:Email";

    private readonly CacheRepositoryBase _cacheRepo;

    public UserRepository(IdentityCoreDbContext dbContext, CacheRepositoryBase cacheRepo) : base(dbContext)
    {
        _cacheRepo = cacheRepo;
    }

    #endregion

    #region Add

    public bool AddToRedis(User user, TimeSpan ttl)
    {
        var keyBaseEntity = $"{RedisKeyPrefixNewUser}:{user.Id}";
        var isUserAdded = _cacheRepo.Add(keyBaseEntity, user, ttl);
        
        var keyUsername = $"{RedisKeyPrefixNewUserName}:{user.Username}"; 
        var isUsernameMapping = _cacheRepo.Add(keyUsername, user.Id.ToString(), ttl);
        
        var keyEmail = $"{RedisKeyPrefixNewUserEmail}:{user.Email}"; 
        var isEmailMapping = _cacheRepo.Add(keyEmail, user.Id.ToString(), ttl);
        
        return isUserAdded && isUsernameMapping && isEmailMapping;
    }

    public async Task<bool> AddedRangeAsync(IEnumerable<User> users)
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

    #endregion

    #region Get
    
    public async Task<User> GetUserFromRedisByIdAsync(Guid id)
    {
        var key = $"{RedisKeyPrefixNewUser}:{id}";
        return await _cacheRepo.GetAsync<User>(key);
    }
    
    public async Task<User> GetUserByIdAsync(Guid id) =>
        await DbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
    
    public async Task<User> GetUserByUsernameAsync(string username) =>
        await DbContext.Users
            .FirstOrDefaultAsync(user => EF.Functions.ILike(user.Username, username));
    
    public async Task<User> GetUserByEmailAsync(string email) =>
        await DbContext.Users
            .FirstOrDefaultAsync(user => EF.Functions.ILike(user.Email, email));

    #endregion

    public async Task<bool> DeleteUserFromRedisAsync(User user)
    {
        var keyBaseEntity = $"{RedisKeyPrefixNewUser}:{user.Id}";
        var isUserRemoved = await _cacheRepo.DeleteAsync(keyBaseEntity);

        var keyUsername = $"{RedisKeyPrefixNewUserName}:{user.Username}";
        var isUsernameRemoved = await _cacheRepo.DeleteAsync(keyUsername);

        var keyEmail = $"{RedisKeyPrefixNewUserEmail}:{user.Email}";
        var isEmailRemoved = await _cacheRepo.DeleteAsync(keyEmail);

        return isUserRemoved && isUsernameRemoved && isEmailRemoved;
    }

    #region Check

    public async Task<bool> UserExistsByUsernameAsync(string username)
    {
        var keyUsername = $"{RedisKeyPrefixNewUserName}:{username}";
        var doesUserExistInRedis = !string.IsNullOrEmpty(await _cacheRepo.GetAsync<string>(keyUsername));
        if (doesUserExistInRedis)
            return true;
        
        return await GetUserByUsernameAsync(username) is not null;
    }

    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        var keyEmail = $"{RedisKeyPrefixNewUserEmail}:{email}";
        var doesUserExistInRedis = !string.IsNullOrEmpty(await _cacheRepo.GetAsync<string>(keyEmail));
        if (doesUserExistInRedis)
            return true;
        
        return await GetUserByEmailAsync(email) is not null;
    }

    #endregion
}