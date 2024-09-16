using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.cache.cachePrefix;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.cache;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.db;
using IdentityCore.Managers.Interfaces;
using IdentityCore.Models;
using IdentityCore.Models.Request;

namespace IdentityCore.Managers;

public class UserManager : IUserManager
{
    #region C-tor and fields

    private readonly IUserDbRepository _userDbRepo;
    private readonly ICacheRepositoryBase _cacheRepo;
    private readonly ICfmTokenCacheRepository _ctRepo;

    public UserManager(IUserDbRepository userDbRepo, ICacheRepositoryBase cacheRepo, ICfmTokenCacheRepository ctRepo)
    {
        _cacheRepo = cacheRepo;
        _userDbRepo = userDbRepo;
        _ctRepo = ctRepo;
    }

    #endregion

    #region CRUD

    public async Task<User> GetUserByIdAsync(Guid id) =>
        await _userDbRepo.GetUserByIdAsync(id);

    public async Task<OperationResult<User>> GetUserSsoAsync(string email)
    {
        var user = await _userDbRepo.GetUserByEmailAsync(email);
        if (user is null
            || (user.Provider == Provider.Local && !await UpdateUserProviderAsync(user, Provider.GoogleWithPass)))
            return new OperationResult<User>("Error updating user to SSO provider");

        return new OperationResult<User>(user);
    }

    public async Task<User> CreateUserForRegistrationAsync(UserCreateRequest userData, Provider provider)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = userData.Username,
            Email = userData.Email,
            Password = null,
            Salt = null,
            Provider = provider
        };

        if (provider != Provider.Local)
        {
            user.IsActive = true;
            return await _userDbRepo.CreateAsync(user);
        }

        user.Salt = UserHelper.GenerateSalt();
        user.Password = UserHelper.GetPasswordHash(userData.Password, user.Salt);
        user.IsActive = false;

        return StoreUserRegistrationInRedis(user, TokenConfig.Values.RegistrationConfirmation) ? user : null;
    }

    public async Task<OperationResult<User>> CreateUserSsoAsync(string email, string name, Provider provider)
    {
        var username = await GenerateUniqueUsernameAsync(name);
        var userData = new UserCreateRequest
        {
            Email = email,
            Username = username,
            Password = null,
            ConfirmPassword = null,
        };

        var newUserSso = await CreateUserForRegistrationAsync(userData, provider);
        return newUserSso == null
            ? new OperationResult<User>("Error creating user")
            : new OperationResult<User>(newUserSso);
    }

    private async Task<bool> UpdateUserProviderAsync(User user, Provider provider)
    {
        user.Provider = provider;
        return await _userDbRepo.UpdateAsync(user);
    }

    public async Task<bool> DeleteUserAsync(User user) =>
        user is not null && await _userDbRepo.DeleteAsync(user);

    #endregion

    #region Redis

    public async Task<User> GetRegUserFromRedisByIdAsync(Guid id)
    {
        var key = $"{RedisPrefixes.User.Registration}:{id}";
        return await _cacheRepo.GetAsync<User>(key);
    }

    public async Task<RedisUserUpdate> GetUpdateUserFromRedisByIdAsync(Guid id)
    {
        var key = $"{RedisPrefixes.User.Update}:{id}";
        return await _cacheRepo.GetAsync<RedisUserUpdate>(key);
    }
    
    public RedisUserUpdate HandleUserUpdateInRedis(UserUpdateRequest updateData, TokenType tokenType)
    {
        var redisUserUpdate = updateData.ToRedisUserUpdate();
        if (tokenType is TokenType.PasswordChange)
        {
            redisUserUpdate.Salt = UserHelper.GenerateSalt();
            redisUserUpdate.Password = UserHelper.GetPasswordHash(updateData.NewPassword, redisUserUpdate.Salt);
        }

        var ttl = TokenConfig.GetTtlForTokenType(tokenType);
        return StoreUserUpdateInRedis(redisUserUpdate, tokenType, ttl) ? redisUserUpdate : null;
    }

    public async Task<bool> UpdateTtlUserUpdateByTokenTypeAsync(
        RedisUserUpdate userData,
        TokenType tokenType,
        TimeSpan ttl)
    {
        var keyBaseEntity = $"{RedisPrefixes.User.Update}:{userData.Id}";
        var isUserUpdateTtl = await _cacheRepo.UpdateTtlAsync(keyBaseEntity, ttl);

        switch (tokenType)
        {
            case TokenType.RegistrationConfirmation:
            {
                var keyRegUsername = $"{RedisPrefixes.User.Name}:{userData.Username}";
                var isUsernameMapping = await _cacheRepo.UpdateTtlAsync(keyRegUsername, ttl);

                var keyEmail = $"{RedisPrefixes.User.Email}:{userData.Email}";
                var isEmailMapping = await _cacheRepo.UpdateTtlAsync(keyEmail, ttl);

                return isUserUpdateTtl && isUsernameMapping && isEmailMapping;
            }
            case TokenType.UsernameChange:
            {
                var keyUsername = $"{RedisPrefixes.User.Name}:{userData.Username}";
                var isUsernameUpdateTtl = await _cacheRepo.UpdateTtlAsync(keyUsername, ttl);
                return isUsernameUpdateTtl && isUserUpdateTtl;
            }
            case TokenType.EmailChangeOld:
            {
                var keyEmail = $"{RedisPrefixes.User.Email}:{userData.Email}";
                var isEmailUpdateTtl = await _cacheRepo.UpdateTtlAsync(keyEmail, ttl);
                return isEmailUpdateTtl && isUserUpdateTtl;
            }
            default:
                return isUserUpdateTtl;
        }
    }

    public async Task<bool> DeleteUserDataFromRedisByTokenTypeAsync(
        Guid id,
        string username,
        string email,
        TokenType tokenType)
    {
        var keyBaseEntity = $"{RedisPrefixes.User.Update}:{id}";
        var isUserRemoved = await _cacheRepo.DeleteAsync(keyBaseEntity);

        switch (tokenType)
        {
            case TokenType.RegistrationConfirmation:
            {
                var keyUsername = $"{RedisPrefixes.User.Name}:{username}";
                var isUsernameRemoved = await _cacheRepo.DeleteAsync(keyUsername);

                var keyEmail = $"{RedisPrefixes.User.Email}:{email}";
                var isEmailRemoved = await _cacheRepo.DeleteAsync(keyEmail);

                return isUserRemoved && isUsernameRemoved && isEmailRemoved;
            }
            case TokenType.UsernameChange:
            {
                var keyUsername = $"{RedisPrefixes.User.Name}:{username}";
                var isUsernameRemoved = await _cacheRepo.DeleteAsync(keyUsername);
 
                return isUserRemoved && isUsernameRemoved;
            }
            case TokenType.EmailChangeOld or TokenType.EmailChangeNew:
            {
                var keyEmail = $"{RedisPrefixes.User.Email}:{email}";
                var isEmailRemoved = await _cacheRepo.DeleteAsync(keyEmail);
                
                return isUserRemoved && isEmailRemoved;
            }
            default:
                return isUserRemoved;
        }
    }
    
    private bool StoreUserRegistrationInRedis(User user, TimeSpan ttl)
    {
        var keyBaseEntity = $"{RedisPrefixes.User.Registration}:{user.Id}";
        var isUserAdded = _cacheRepo.Add(keyBaseEntity, user, ttl);

        var keyUsername = $"{RedisPrefixes.User.Name}:{user.Username}";
        var isUsernameMapping = _cacheRepo.Add(keyUsername, user.Id.ToString(), ttl);

        var keyEmail = $"{RedisPrefixes.User.Email}:{user.Email}";
        var isEmailMapping = _cacheRepo.Add(keyEmail, user.Id.ToString(), ttl);

        return isUserAdded && isUsernameMapping && isEmailMapping;
    }
    
    private bool StoreUserUpdateInRedis(RedisUserUpdate updateRequest, TokenType tokenType, TimeSpan ttl)
    {
        var keyBaseEntity = $"{RedisPrefixes.User.Update}:{updateRequest.Id}";
        var isUserUpdateRequestAdded = _cacheRepo.Add(keyBaseEntity, updateRequest, ttl);
        switch (tokenType)
        {
            case TokenType.UsernameChange:
            {
                var keyUsername = $"{RedisPrefixes.User.Name}:{updateRequest.Username}";
                var isUsernameMapping = _cacheRepo.Add(keyUsername, updateRequest.Username, ttl);
                return isUsernameMapping && isUserUpdateRequestAdded;
            }
            case TokenType.EmailChangeOld:
            {
                var keyEmail = $"{RedisPrefixes.User.Email}:{updateRequest.Email}";
                var isEmailMapping = _cacheRepo.Add(keyEmail, updateRequest.Email, ttl);
                return isEmailMapping && isUserUpdateRequestAdded;
            }
            default:
                return isUserUpdateRequestAdded;
        }
    }
    
    #endregion

    #region Token-Based Update Handling

    public async Task<string> ExecuteUserUpdateFromTokenAsync(
        User user,
        RedisUserUpdate userUpdate,
        RedisConfirmationToken token)
    {
        return token.TokenType switch
        {
            TokenType.RegistrationConfirmation => await HandleRegistrationConfirmation(user, token),
            TokenType.EmailChangeOld => await HandleEmailChangeOld(userUpdate, token),
            TokenType.EmailChangeNew => await HandleEmailChangeNew(user, userUpdate, token),
            TokenType.PasswordChange => await HandlePasswordChange(user, userUpdate, token),
            TokenType.UsernameChange => await HandleUsernameChange(user, userUpdate, token),
            _ => throw new ArgumentException("Unknown TokenType")
        };
    }

    private async Task<string> HandleRegistrationConfirmation(User user, RedisConfirmationToken token)
    {
        var isTokenRemoved = await _ctRepo.DeleteFromRedisAsync(token);
        var isUserRemoved = await DeleteUserDataFromRedisByTokenTypeAsync(
            user.Id,
            user.Username,
            user.Email,
            token.TokenType);
        
        if (!isTokenRemoved || !isUserRemoved)
            return "Activation error";

        user.IsActive = true;
        return await _userDbRepo.CreateAsync(user) is not null
            ? string.Empty
            : "Activation error";
    }

    private async Task<string> HandleEmailChangeOld(RedisUserUpdate userUpdate, RedisConfirmationToken token)
    {
        var isRemoved = await _ctRepo.DeleteFromRedisAsync(token);
        if (!isRemoved)
            return "Error changing email";

        token.TokenType = TokenType.EmailChangeNew;
        var ttl = TokenConfig.GetTtlForTokenType(TokenType.EmailChangeNew);
        var isTokenUpdated = _ctRepo.AddToRedis(token, ttl);
        var isUserUpdated = await UpdateTtlUserUpdateByTokenTypeAsync(userUpdate, token.TokenType, ttl);

        if (isTokenUpdated && isUserUpdated)
            return string.Empty;

        _ = await _ctRepo.DeleteFromRedisAsync(token);
        _ = await DeleteUserDataFromRedisByTokenTypeAsync(userUpdate.Id, null, userUpdate.Email, token.TokenType);

        return "Error changing email";
    }

    private async Task<string> HandleEmailChangeNew(User user, RedisUserUpdate userUpdate, RedisConfirmationToken token)
    {
        var isTokenRemoved = await _ctRepo.DeleteFromRedisAsync(token);
        var isUserRemoved =
            await DeleteUserDataFromRedisByTokenTypeAsync(userUpdate.Id, null, userUpdate.Email, token.TokenType);
        
        if (!isTokenRemoved || !isUserRemoved || string.IsNullOrEmpty(userUpdate.Email))
            return "An error occurred while changing email";

        user.Email = userUpdate.Email;
        return await _userDbRepo.UpdateAsync(user)
            ? string.Empty
            : "An error occurred while changing email";
    }

    private async Task<string> HandlePasswordChange(User user, RedisUserUpdate userUpdate, RedisConfirmationToken token)
    {
        var isTokenRemoved = await _ctRepo.DeleteFromRedisAsync(token);
        var isUserRemoved =
            await DeleteUserDataFromRedisByTokenTypeAsync(userUpdate.Id, null, null, token.TokenType);
        
        if (!isTokenRemoved
            || !isUserRemoved
            || string.IsNullOrEmpty(userUpdate.Password)
            || string.IsNullOrEmpty(userUpdate.Salt))
            return "An error occurred while changing password";

        user.Password = userUpdate.Password;
        user.Salt = userUpdate.Salt;
        return await _userDbRepo.UpdateAsync(user)
            ? string.Empty
            : "An error occurred while changing password";
    }

    private async Task<string> HandleUsernameChange(User user, RedisUserUpdate userUpdate, RedisConfirmationToken token)
    {
        var isTokenRemoved = await _ctRepo.DeleteFromRedisAsync(token);
        var isUserRemoved =
            await DeleteUserDataFromRedisByTokenTypeAsync(userUpdate.Id, userUpdate.Username, null, token.TokenType);
        
        if (!isTokenRemoved || !isUserRemoved || string.IsNullOrEmpty(userUpdate.Username))
            return "An error occurred while changing username";

        user.Username = userUpdate.Username;
        return await _userDbRepo.UpdateAsync(user)
            ? string.Empty
            : "An error occurred while changing username";
    }

    #endregion

    #region Validation

    public async Task<OperationResult<User>> ValidateUserUpdateAsync(UserUpdateRequest updateData)
    {
        if (!IsSingleFieldProvided(updateData))
            return new OperationResult<User>("Only one field can be provided for update");

        if (!string.IsNullOrWhiteSpace(updateData.Email)
            && await UserExistsByEmailAsync(updateData.Email))
            return new OperationResult<User>("Email is already taken");

        if (!string.IsNullOrWhiteSpace(updateData.Username)
            && await UserExistsByUsernameAsync(updateData.Username))
            return new OperationResult<User>("A user with this username exists");

        var user = await _userDbRepo.GetUserByIdAsync(updateData.Id);
        if (user is null
            || (user.Provider == Provider.Google && !string.IsNullOrWhiteSpace(updateData.OldPassword))
            || (user.Provider != Provider.Local && !string.IsNullOrWhiteSpace(updateData.Email)))
            return new OperationResult<User>("Invalid input data");

        if (string.IsNullOrWhiteSpace(updateData.OldPassword))
            return new OperationResult<User>(user);

        var hashCurrentPassword = UserHelper.GetPasswordHash(updateData.OldPassword, user.Salt!);
        return !hashCurrentPassword.Equals(user.Password)
            ? new OperationResult<User>("Invalid input data")
            : new OperationResult<User>(user);
    }

    public async Task<OperationResult<User>> ValidateLoginAsync(UserLoginRequest loginRequest)
    {
        if (string.IsNullOrWhiteSpace(loginRequest.Email) || string.IsNullOrWhiteSpace(loginRequest.Password))
            return new OperationResult<User>("Email or password is invalid");

        var user = await _userDbRepo.GetUserByEmailAsync(loginRequest.Email);
        if (user is null || user.Provider == Provider.Google)
            return new OperationResult<User>("Email or password is invalid");

        var userPasswordHash = UserHelper.GetPasswordHash(loginRequest.Password, user.Salt!);
        return userPasswordHash.Equals(user.Password)
            ? new OperationResult<User>(user)
            : new OperationResult<User>("Email or password is invalid");
    }

    public async Task<string> ValidateRegistrationAsync(UserCreateRequest userCreateRequest)
    {
        if (string.IsNullOrWhiteSpace(userCreateRequest.Email)
            || string.IsNullOrWhiteSpace(userCreateRequest.Username)
            || string.IsNullOrWhiteSpace(userCreateRequest.Password))
            return "Invalid input data";

        if (await UserExistsByEmailAsync(userCreateRequest.Email))
            return "Email is already taken";

        if (await UserExistsByUsernameAsync(userCreateRequest.Username))
            return "A user with this username exists";

        return string.Empty;
    }

    public async Task<bool> IsUserUpdateInProgressAsync(Guid id)
    {
        var key = $"{RedisPrefixes.User.Update}:{id}";
        return await _cacheRepo.KeyExistsAsync(key);
    }

    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        var keyEmail = $"{RedisPrefixes.User.Email}:{email}";
        var doesUserExistInRedis = !string.IsNullOrEmpty(await _cacheRepo.GetAsync<string>(keyEmail));
        if (doesUserExistInRedis)
            return true;

        return await _userDbRepo.GetUserByEmailAsync(email) is not null;
    }

    private static bool IsSingleFieldProvided(UserUpdateRequest updateRequest)
    {
        var filledFieldsCount = 0;

        if (!string.IsNullOrWhiteSpace(updateRequest.Username)) filledFieldsCount++;
        if (!string.IsNullOrWhiteSpace(updateRequest.Email)) filledFieldsCount++;
        if (!string.IsNullOrWhiteSpace(updateRequest.OldPassword)) filledFieldsCount++;

        return filledFieldsCount == 1;
    }
    
    private async Task<bool> UserExistsByUsernameAsync(string username)
    {
        var keyUsername = $"{RedisPrefixes.User.Name}:{username}";
        var doesUserExistInRedis = !string.IsNullOrEmpty(await _cacheRepo.GetAsync<string>(keyUsername));
        if (doesUserExistInRedis)
            return true;

        return await _userDbRepo.GetUserByUsernameAsync(username) is not null;
    }

    #endregion

    #region Common

    private async Task<string> GenerateUniqueUsernameAsync(string username)
    {
        username = username.Replace(" ", "_");
        if (!await UserExistsByUsernameAsync(username))
            return username;

        var random = new Random();
        do
        {
            var newUsername = $"{username}_{random.Next(0, 10000):D4}";
            if (!await UserExistsByUsernameAsync(newUsername))
                return newUsername;
        } while (true);
    }

    #endregion
}