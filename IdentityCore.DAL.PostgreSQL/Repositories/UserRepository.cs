using Microsoft.EntityFrameworkCore;
using IdentityCore.DAL.PostgreSQL.Models;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Repositories.Base;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.Base;

namespace IdentityCore.DAL.PostgreSQL.Repositories;

public class UserRepository : DbRepositoryBase<User>, IUserRepository
{
    #region C-tor and fields

    private const string RedisKeyPrefixRegUser = "UR";
    private const string RedisKeyPrefixUpdateUser = "UU";
    private const string RedisKeyPrefixUserName = "Username";
    private const string RedisKeyPrefixUserEmail = "Email";

    private readonly ICacheRepositoryBase _cacheRepo;

    public UserRepository(IdentityCoreDbContext dbContext, ICacheRepositoryBase cacheRepo) : base(dbContext)
    {
        _cacheRepo = cacheRepo;
    }

    #endregion

    #region Get

    public async Task<User> GetRegUserFromRedisByIdAsync(Guid id)
    {
        var key = $"{RedisKeyPrefixRegUser}:{id}";
        return await _cacheRepo.GetAsync<User>(key);
    }

    public async Task<RedisUserUpdate> GetUpdateUserFromRedisByIdAsync(Guid id)
    {
        var key = $"{RedisKeyPrefixUpdateUser}:{id}";
        return await _cacheRepo.GetAsync<RedisUserUpdate>(key);
    }

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
    
    #endregion

    #region Add

    public bool AddRegUserToRedis(User user, TimeSpan ttl)
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

    #region Delete

    public async Task<bool> DeleteRegUserFromRedisAsync(User user)
    {
        var keyBaseEntity = $"{RedisKeyPrefixRegUser}:{user.Id}";
        var isUserRemoved = await _cacheRepo.DeleteAsync(keyBaseEntity);

        var keyUsername = $"{RedisKeyPrefixUserName}:{user.Username}";
        var isUsernameRemoved = await _cacheRepo.DeleteAsync(keyUsername);

        var keyEmail = $"{RedisKeyPrefixUserEmail}:{user.Email}";
        var isEmailRemoved = await _cacheRepo.DeleteAsync(keyEmail);

        return isUserRemoved && isUsernameRemoved && isEmailRemoved;
    }

    public async Task<bool> DeleteUserUpdateDataFromRedisAsync(RedisUserUpdate update, TokenType tokenType)
    {
        var keyBaseEntity = $"{RedisKeyPrefixUpdateUser}:{update.Id}";
        var isUserUpdateDataRemoved = await _cacheRepo.DeleteAsync(keyBaseEntity);

        var additionalKey = tokenType switch
        {
            TokenType.UsernameChange => $"{RedisKeyPrefixUserName}:{update.Username}",
            TokenType.EmailChangeOld or TokenType.EmailChangeNew => $"{RedisKeyPrefixUserEmail}:{update.Email}",
            _ => null
        };

        if (string.IsNullOrEmpty(additionalKey))
            return isUserUpdateDataRemoved;

        var isAdditionalKeyRemoved = await _cacheRepo.DeleteAsync(additionalKey);
        return isUserUpdateDataRemoved && isAdditionalKeyRemoved;
    }

    #endregion

    #region Update

    public async Task<bool> UpdateTtlRegUserAsync(User user, TimeSpan ttl)
    {
        var keyBaseEntity = $"{RedisKeyPrefixRegUser}:{user.Id}";
        var isUserAdded = await _cacheRepo.UpdateTtlAsync(keyBaseEntity, ttl);

        var keyUsername = $"{RedisKeyPrefixUserName}:{user.Username}";
        var isUsernameMapping = await _cacheRepo.UpdateTtlAsync(keyUsername, ttl);

        var keyEmail = $"{RedisKeyPrefixUserEmail}:{user.Email}";
        var isEmailMapping = await _cacheRepo.UpdateTtlAsync(keyEmail, ttl);

        return isUserAdded && isUsernameMapping && isEmailMapping;
    }

    public async Task<bool> UpdateTtlUserUpdateAsync(RedisUserUpdate updateRequest, TokenType tokenType, TimeSpan ttl)
    {
        var keyBaseEntity = $"{RedisKeyPrefixUpdateUser}:{updateRequest.Id}";
        var isUserUpdateTtl = await _cacheRepo.UpdateTtlAsync(keyBaseEntity, ttl);

        switch (tokenType)
        {
            case TokenType.UsernameChange:
            {
                var keyUsername = $"{RedisKeyPrefixUserName}:{updateRequest.Username}";
                var isUsernameUpdateTtl = await _cacheRepo.UpdateTtlAsync(keyUsername, ttl);
                return isUsernameUpdateTtl && isUserUpdateTtl;
            }
            case TokenType.EmailChangeOld:
            {
                var keyEmail = $"{RedisKeyPrefixUserEmail}:{updateRequest.Email}";
                var isEmailUpdateTtl = await _cacheRepo.UpdateTtlAsync(keyEmail, ttl);
                return isEmailUpdateTtl && isUserUpdateTtl;
            }
            default:
                return isUserUpdateTtl;
        }
    }

    #endregion

    #region Check

    public async Task<bool> IsUserUpdateInProgress(Guid id)
    {
        var key = $"{RedisKeyPrefixUpdateUser}:{id}";
        return await _cacheRepo.KeyExistsAsync(key);
    }

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