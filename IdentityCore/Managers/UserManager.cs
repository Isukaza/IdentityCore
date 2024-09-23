using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.cache.cachePrefix;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.cache;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.db;
using IdentityCore.DAL.PostgreSQL.Roles;
using IdentityCore.Managers.Interfaces;
using IdentityCore.Models;
using IdentityCore.Models.Request;

namespace IdentityCore.Managers;

public class UserManager : IUserManager
{
    #region C-tor and fields

    private readonly IUserDbRepository _userDbRepo;
    private readonly IUserCacheRepository _userCacheRepo;
    private readonly ICfmTokenCacheRepository _ctCacheRepo;

    public UserManager(
        IUserDbRepository userDbRepo,
        IUserCacheRepository userCacheRepo,
        ICfmTokenCacheRepository ctCacheRepo)
    {
        _userCacheRepo = userCacheRepo;
        _userDbRepo = userDbRepo;
        _ctCacheRepo = ctCacheRepo;
    }

    #endregion

    #region CRUD

    public async Task<User> GetUserByIdAsync(Guid id) =>
        await _userDbRepo.GetUserByIdAsync(id);

    public async Task<User> GetUserByTokenTypeAsync(Guid id, TokenType tokenType) =>
        tokenType == TokenType.RegistrationConfirmation
            ? await _userCacheRepo.GetUserByIdAsync<User>(RedisPrefixes.User.Registration, id)
            : await _userDbRepo.GetUserByIdAsync(id);

    public async Task<T> GetUserByIdAsync<T>(string prefix, Guid id) =>
        await _userCacheRepo.GetUserByIdAsync<T>(prefix, id);

    public async Task<OperationResult<User>> GetUserSsoAsync(string email)
    {
        var user = await _userDbRepo.GetUserByEmailAsync(email);
        if (user is null
            || (user.Provider == Provider.Local && !await UpdateUserProviderAsync(user, Provider.GoogleWithPass)))
            return new OperationResult<User>("Error updating user to SSO provider");

        return new OperationResult<User>(user);
    }

    public RedisUserUpdate AddUserUpdateDataByTokenType(RedisUserUpdate userUpdateData, TokenType tokenType,
        TimeSpan ttl)
    {
        var isUserUpdateRequestAdded = _userCacheRepo
            .AddEntityToCache(RedisPrefixes.User.Update, userUpdateData.Id, userUpdateData, ttl);
        if (!isUserUpdateRequestAdded)
            return null;

        bool isAdditionalMappingUpdated;
        switch (tokenType)
        {
            case TokenType.UsernameChange:
            {
                isAdditionalMappingUpdated = _userCacheRepo
                    .AddMappingToCache(RedisPrefixes.User.Name, userUpdateData.Username, ttl);
                break;
            }
            case TokenType.EmailChangeOld:
            {
                isAdditionalMappingUpdated = _userCacheRepo
                    .AddMappingToCache(RedisPrefixes.User.Email, userUpdateData.Email, ttl);
                break;
            }
            default:
                isAdditionalMappingUpdated = true;
                break;
        }

        return isAdditionalMappingUpdated ? userUpdateData : null;
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
            Role = UserRole.User,
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

        return AddRegisteredUser(user, TokenConfig.Values.RegistrationConfirmation) ? user : null;
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

    private bool AddRegisteredUser(User user, TimeSpan ttl)
    {
        var isUserAdded = _userCacheRepo.AddEntityToCache(RedisPrefixes.User.Registration, user.Id, user, ttl);
        var isUsernameMapping = _userCacheRepo.AddMappingToCache(RedisPrefixes.User.Name, user.Username, ttl);
        var isEmailMapping = _userCacheRepo.AddMappingToCache(RedisPrefixes.User.Email, user.Email, ttl);

        return isUserAdded && isUsernameMapping && isEmailMapping;
    }

    private async Task<bool> UpdateUserProviderAsync(User user, Provider provider)
    {
        user.Provider = provider;
        return await _userDbRepo.UpdateAsync(user);
    }

    public async Task<bool> DeleteUserAsync(User user) =>
        user is not null && await _userDbRepo.DeleteAsync(user);

    public async Task<bool> UpdateTtlUserUpdateByTokenTypeAsync(
        RedisUserUpdate userData,
        TokenType tokenType,
        TimeSpan ttl)
    {
        var userCachePrefix = tokenType == TokenType.RegistrationConfirmation
            ? RedisPrefixes.User.Registration
            : RedisPrefixes.User.Update;

        var tasks = new List<Task<bool>>
        {
            _userCacheRepo.UpdateTtlEntityInCache(userCachePrefix, userData.Id.ToString(), ttl)
        };

        switch (tokenType)
        {
            case TokenType.RegistrationConfirmation:
            {
                tasks.Add(_userCacheRepo.UpdateTtlEntityInCache(RedisPrefixes.User.Name, userData.Username, ttl));
                tasks.Add(_userCacheRepo.UpdateTtlEntityInCache(RedisPrefixes.User.Email, userData.Email, ttl));
                break;
            }
            case TokenType.UsernameChange:
            {
                tasks.Add(_userCacheRepo.UpdateTtlEntityInCache(RedisPrefixes.User.Name, userData.Username, ttl));
                break;
            }
            case TokenType.EmailChangeOld:
            {
                tasks.Add(_userCacheRepo.UpdateTtlEntityInCache(RedisPrefixes.User.Email, userData.Email, ttl));
                break;
            }
        }

        var results = await Task.WhenAll(tasks);
        return results.All(result => result);
    }

    public async Task<bool> DeleteUserDataByTokenTypeAsync(
        Guid id,
        string username,
        string email,
        TokenType tokenType)
    {
        var userCachePrefix = tokenType == TokenType.RegistrationConfirmation
            ? RedisPrefixes.User.Registration
            : RedisPrefixes.User.Update;

        var tasks = new List<Task<bool>>
        {
            _userCacheRepo.RemoveEntityFromCache(userCachePrefix, id.ToString())
        };

        switch (tokenType)
        {
            case TokenType.RegistrationConfirmation:
            {
                tasks.Add(_userCacheRepo.RemoveEntityFromCache(RedisPrefixes.User.Name, username));
                tasks.Add(_userCacheRepo.RemoveEntityFromCache(RedisPrefixes.User.Email, email));
                break;
            }
            case TokenType.UsernameChange:
            {
                tasks.Add(_userCacheRepo.RemoveEntityFromCache(RedisPrefixes.User.Name, username));
                break;
            }
            case TokenType.EmailChangeOld or TokenType.EmailChangeNew:
            {
                tasks.Add(_userCacheRepo.RemoveEntityFromCache(RedisPrefixes.User.Email, email));
                break;
            }
        }

        var results = await Task.WhenAll(tasks);
        return results.All(result => result);
    }

    #endregion

    #region Token-Based Update Handling

    public async Task<string> ExecuteUserUpdateFromTokenAsync(
        User user,
        RedisUserUpdate userUpdData,
        RedisConfirmationToken token)
    {
        return token.TokenType switch
        {
            TokenType.RegistrationConfirmation => await HandleRegistrationConfirmation(user),
            TokenType.EmailChangeOld => HandleEmailChangeOld(token),
            TokenType.EmailChangeNew => await HandleEmailChangeNew(user, userUpdData),
            TokenType.PasswordChange => await HandlePasswordChange(user, userUpdData),
            TokenType.UsernameChange => await HandleUsernameChange(user, userUpdData),
            _ => "Invalid token"
        };
    }

    private async Task<string> HandleRegistrationConfirmation(User user)
    {
        user.IsActive = true;
        return await _userDbRepo.CreateAsync(user) is not null
            ? string.Empty
            : "Activation error";
    }

    private string HandleEmailChangeOld(RedisConfirmationToken token)
    {
        var tokenEmailNew = new RedisConfirmationToken
        {
            UserId = token.UserId,
            Value = UserHelper.GetToken(token.UserId),
            TokenType = TokenType.EmailChangeNew,
            AttemptCount = 1,
            Modified = token.Modified
        };

        var ttl = TokenConfig.GetTtlForTokenType(tokenEmailNew.TokenType);
        var isTokenAdded = _ctCacheRepo.Add(tokenEmailNew.Value, tokenEmailNew, tokenEmailNew.TokenType, ttl);
        var isTokenUserIdAdded = _ctCacheRepo
            .Add(tokenEmailNew.UserId.ToString(), tokenEmailNew.Value, tokenEmailNew.TokenType, ttl);

        return isTokenAdded && isTokenUserIdAdded ? string.Empty : "Error changing email";
    }

    private async Task<string> HandleEmailChangeNew(User user, RedisUserUpdate userUpdateData)
    {
        if (string.IsNullOrEmpty(userUpdateData.Email))
            return "An error occurred while changing email";

        user.Email = userUpdateData.Email;
        return await _userDbRepo.UpdateAsync(user)
            ? string.Empty
            : "An error occurred while changing email";
    }

    private async Task<string> HandlePasswordChange(User user, RedisUserUpdate userUpdateData)
    {
        if (string.IsNullOrEmpty(userUpdateData.Password) || string.IsNullOrEmpty(userUpdateData.Salt))
            return "An error occurred while changing password";

        user.Password = userUpdateData.Password;
        user.Salt = userUpdateData.Salt;
        return await _userDbRepo.UpdateAsync(user)
            ? string.Empty
            : "An error occurred while changing password";
    }

    private async Task<string> HandleUsernameChange(User user, RedisUserUpdate userUpdateData)
    {
        if (string.IsNullOrEmpty(userUpdateData.Username))
            return "An error occurred while changing username";

        user.Username = userUpdateData.Username;
        return await _userDbRepo.UpdateAsync(user)
            ? string.Empty
            : "An error occurred while changing username";
    }

    #endregion

    #region Validation

    public async Task<bool> IsUserUpdateInProgressAsync(Guid id) =>
        await _userCacheRepo.IsUserUpdateInProgressAsync(id);

    public async Task<bool> UserExistsByEmailAsync(string email) =>
        await _userCacheRepo.UserExistsByEmailAsync(email)
        || await _userDbRepo.GetUserByEmailAsync(email) is not null;

    public async Task<OperationResult<User>> ValidateUserUpdateAsync(UserUpdateRequest updateData)
    {
        if (!IsSingleFieldProvided(updateData))
            return new OperationResult<User>("Only one field can be provided for update");

        if (!string.IsNullOrWhiteSpace(updateData.Email) && await UserExistsByEmailAsync(updateData.Email))
            return new OperationResult<User>("Email is already taken");

        if (!string.IsNullOrWhiteSpace(updateData.Username) && await UserExistsByUsernameAsync(updateData.Username))
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

    private static bool IsSingleFieldProvided(UserUpdateRequest updateRequest)
    {
        var filledFieldsCount = 0;

        if (!string.IsNullOrWhiteSpace(updateRequest.Username)) filledFieldsCount++;
        if (!string.IsNullOrWhiteSpace(updateRequest.Email)) filledFieldsCount++;
        if (!string.IsNullOrWhiteSpace(updateRequest.OldPassword)) filledFieldsCount++;

        return filledFieldsCount == 1;
    }

    private async Task<bool> UserExistsByUsernameAsync(string username) =>
        await _userCacheRepo.UserExistsByUsernameAsync(username)
        || await _userDbRepo.GetUserByUsernameAsync(username) is not null;

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