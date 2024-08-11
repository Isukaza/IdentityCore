using Microsoft.EntityFrameworkCore;

using IdentityCore.DAL.PorstgreSQL;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository.Base;

namespace IdentityCore.DAL.Repository;

public class UserRepository : DbRepositoryBase<User>
{
    #region C-tor and Fields

    private const string RedisKeyPrefixRegUser = "UR";
    private const string RedisKeyPrefixUpdateUser = "UU";
    private const string RedisKeyPrefixUserName = "Username";
    private const string RedisKeyPrefixUserEmail = "Email";

    private readonly CacheRepositoryBase _cacheRepo;

    public UserRepository(IdentityCoreDbContext dbContext, CacheRepositoryBase cacheRepo) : base(dbContext)
    {
        _cacheRepo = cacheRepo;
    }

    #endregion

    #region Add

    public bool AddToRedis(User user, TimeSpan ttl)
    {
        var keyBaseEntity = $"{RedisKeyPrefixRegUser}:{user.Id}";
        var isUserAdded = _cacheRepo.Add(keyBaseEntity, user, ttl);
        
        var keyUsername = $"{RedisKeyPrefixUserName}:{user.Username}"; 
        var isUsernameMapping = _cacheRepo.Add(keyUsername, user.Id.ToString(), ttl);
        
        var keyEmail = $"{RedisKeyPrefixUserEmail}:{user.Email}"; 
        var isEmailMapping = _cacheRepo.Add(keyEmail, user.Id.ToString(), ttl);
        
        return isUserAdded && isUsernameMapping && isEmailMapping;
    }
    
    public bool AddToRedisUpdateRequest(RedisUserUpdate updateRequest, TokenType tokenType, TimeSpan ttl)
    {
        var keyBaseEntity = $"{RedisKeyPrefixUpdateUser}:{updateRequest.Id}";
        var isUserUpdateRequestAdded = _cacheRepo.Add(keyBaseEntity, updateRequest, ttl);

        switch (tokenType)
        {
            case TokenType.UsernameChange:
            {
                var keyUsername = $"{RedisKeyPrefixUserName}:{updateRequest.Username}"; 
                var isUsernameMapping = _cacheRepo.Add(keyUsername, updateRequest.Username, ttl);
                return isUsernameMapping && isUserUpdateRequestAdded;
            }
            case TokenType.EmailChangeOld:
            {
                var keyEmail = $"{RedisKeyPrefixUserEmail}:{updateRequest.Email}"; 
                var isEmailMapping = _cacheRepo.Add(keyEmail, updateRequest.Email, ttl);
                return isEmailMapping && isUserUpdateRequestAdded;
            }
            default:
                return isUserUpdateRequestAdded;
        }
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
    
    public async Task<User> GetUserFromRedisByIdAsync(Guid id, TokenType tokenType)
    {
        var redisPrefix = tokenType == TokenType.RegistrationConfirmation
            ? RedisKeyPrefixRegUser
            : RedisKeyPrefixUpdateUser;
        
        var key = $"{redisPrefix}:{id}";
        return await _cacheRepo.GetAsync<User>(key);
    }
    
    public async Task<User> GetUserByIdAsync(Guid id) =>
        await DbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
    
    public async Task<User> GetUserByUsernameAsync(string username) =>
        await DbContext.Users.FirstOrDefaultAsync(u => EF.Functions.ILike(u.Username, username));
    
    public async Task<User> GetUserByEmailAsync(string email) =>
        await DbContext.Users.FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email, email));

    #endregion

    public async Task<bool> DeleteRegisteredUserFromRedisAsync(User user)
    {
        var keyBaseEntity = $"{RedisKeyPrefixRegUser}:{user.Id}";
        var isUserRemoved = await _cacheRepo.DeleteAsync(keyBaseEntity);

        var keyUsername = $"{RedisKeyPrefixUserName}:{user.Username}";
        var isUsernameRemoved = await _cacheRepo.DeleteAsync(keyUsername);

        var keyEmail = $"{RedisKeyPrefixUserEmail}:{user.Email}";
        var isEmailRemoved = await _cacheRepo.DeleteAsync(keyEmail);

        return isUserRemoved && isUsernameRemoved && isEmailRemoved;
    }

    #region Check

    public async Task<bool> IsUserUpdateInProgress(Guid id)
    {
        var key = $"{RedisKeyPrefixUpdateUser}:{id}";
        return await _cacheRepo.KeyExistsAsync(key);
    }
    public async Task<bool> UserExistsAsync(Guid userId) => 
        await DbContext.Users.AnyAsync(u => u.Id == userId);
    
    public async Task<bool> UserExistsByUsernameAsync(string username)
    {
        var keyUsername = $"{RedisKeyPrefixUserName}:{username}";
        var doesUserExistInRedis = !string.IsNullOrEmpty(await _cacheRepo.GetAsync<string>(keyUsername));
        if (doesUserExistInRedis)
            return true;
        
        return await GetUserByUsernameAsync(username) is not null;
    }

    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        var keyEmail = $"{RedisKeyPrefixUserEmail}:{email}";
        var doesUserExistInRedis = !string.IsNullOrEmpty(await _cacheRepo.GetAsync<string>(keyEmail));
        if (doesUserExistInRedis)
            return true;
        
        return await GetUserByEmailAsync(email) is not null;
    }

    #endregion
}