using System.Security.Claims;
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
using IdentityCore.Models.Interface;
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

    public async Task<User> GetUserByEmailAsync(string email) =>
        await _userDbRepo.GetUserByEmailAsync(email);

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

    public bool AddUserUpdateDataToRedis(RedisUserUpdate userUpdate)
    {
        var ttl = TokenConfig.GetTtlForTokenType(userUpdate.ChangeType);
        var isUserUpdateRequestAdded = _userCacheRepo
            .AddEntityToCache(RedisPrefixes.User.Update, userUpdate.Id, userUpdate, ttl);
        if (!isUserUpdateRequestAdded)
            return false;

        bool isAdditionalMappingUpdated;
        switch (userUpdate.ChangeType)
        {
            case TokenType.UsernameChange:
            {
                isAdditionalMappingUpdated = _userCacheRepo
                    .AddMappingToCache(RedisPrefixes.User.Name, userUpdate.NewValue, ttl);
                break;
            }
            case TokenType.EmailChangeOld:
            {
                isAdditionalMappingUpdated = _userCacheRepo
                    .AddMappingToCache(RedisPrefixes.User.Email, userUpdate.NewValue, ttl);
                break;
            }
            default:
                isAdditionalMappingUpdated = true;
                break;
        }

        return isAdditionalMappingUpdated;
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

    public async Task<bool> UpdateUser(User user, SuUserUpdateRequest updateData)
    {
        if (user is null || updateData is null || !IsAnyFieldProvided(updateData))
            return false;

        if (!string.IsNullOrWhiteSpace(updateData.Username))
        {
            if (await UserExistsByUsernameAsync(updateData.Username))
                return false;

            user.Username = updateData.Username;
        }

        if (!string.IsNullOrWhiteSpace(updateData.Email))
        {
            if (await UserExistsByEmailAsync(updateData.Email))
                return false;

            user.Email = updateData.Email;
        }

        if (updateData.Role.HasValue)
            user.Role = updateData.Role.Value;

        if (!string.IsNullOrWhiteSpace(updateData.NewPassword))
        {
            user.Salt = UserHelper.GenerateSalt();
            user.Password = UserHelper.GetPasswordHash(updateData.NewPassword, user.Salt);
        }

        return await _userDbRepo.UpdateAsync(user);
    }
    
    public RedisUserUpdate CreateUserUpdateEntity(IUserUpdate updateRequest, User user)
    {
        var updateData = new RedisUserUpdate
        {
            Id = updateRequest.Id,
            ChangeType = TokenType.Unknown
        };
        
        if (!string.IsNullOrWhiteSpace(updateRequest.Username))
        {
            updateData.ChangeType = TokenType.UsernameChange;
            updateData.NewValue = updateRequest.Username;
        }
        else if (!string.IsNullOrWhiteSpace(updateRequest.NewPassword))
        {
            updateData.ChangeType = TokenType.PasswordChange;
            updateData.Salt = UserHelper.GenerateSalt(); 
            updateData.NewValue =  UserHelper.GetPasswordHash(updateRequest.NewPassword, updateData.Salt);
        }
        else if (!string.IsNullOrWhiteSpace(updateRequest.Email))
        {
            updateData.ChangeType = TokenType.EmailChangeOld;
            updateData.NewValue = updateRequest.Email;
        }

        return updateData;
    }

    private async Task<bool> UpdateUserProviderAsync(User user, Provider provider)
    {
        user.Provider = provider;
        return await _userDbRepo.UpdateAsync(user);
    }

    public async Task<bool> DeleteUserAsync(User user) =>
        user is not null && await _userDbRepo.DeleteAsync(user);

    public async Task<bool> UpdateTtlUserUpdateByTokenTypeAsync(
        Guid userId,
        string username,
        string email,
        TokenType tokenType)
    {
        var ttl = TokenConfig.GetTtlForTokenType(tokenType);
        var userCachePrefix = tokenType == TokenType.RegistrationConfirmation
            ? RedisPrefixes.User.Registration
            : RedisPrefixes.User.Update;

        var tasks = new List<Task<bool>>
        {
            _userCacheRepo.UpdateTtlEntityInCache(userCachePrefix, userId.ToString(), ttl)
        };

        switch (tokenType)
        {
            case TokenType.RegistrationConfirmation:
            {
                tasks.Add(_userCacheRepo.UpdateTtlEntityInCache(RedisPrefixes.User.Name, username, ttl));
                tasks.Add(_userCacheRepo.UpdateTtlEntityInCache(RedisPrefixes.User.Email, email, ttl));
                break;
            }
            case TokenType.UsernameChange:
            {
                tasks.Add(_userCacheRepo.UpdateTtlEntityInCache(RedisPrefixes.User.Name, username, ttl));
                break;
            }
            case TokenType.EmailChangeOld:
            {
                tasks.Add(_userCacheRepo.UpdateTtlEntityInCache(RedisPrefixes.User.Email, email, ttl));
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
        RedisUserUpdate userUpd,
        RedisConfirmationToken token)
    {
        if (token is null)
            return "Invalid token";

        return token.TokenType switch
        {
            TokenType.RegistrationConfirmation => await HandleRegistrationConfirmation(user),
            TokenType.EmailChangeOld => HandleEmailChangeOld(token),
            TokenType.EmailChangeNew => await HandleEmailChangeNew(user, userUpd),
            TokenType.PasswordChange or TokenType.PasswordReset => await HandlePasswordChange(user, userUpd),
            TokenType.UsernameChange => await HandleUsernameChange(user, userUpd),
            _ => "Invalid token"
        };
    }

    private async Task<string> HandleRegistrationConfirmation(User user)
    {
        if (user is null)
            return "Activation error";

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

        Console.WriteLine(tokenEmailNew.Value);
        return isTokenAdded && isTokenUserIdAdded ? string.Empty : "Error changing email";
    }

    private async Task<string> HandleEmailChangeNew(User user, RedisUserUpdate userUpdate)
    {
        if (user is null || string.IsNullOrEmpty(userUpdate?.NewValue))
            return "An error occurred while changing email";

        user.Email = userUpdate.NewValue;
        return await _userDbRepo.UpdateAsync(user)
            ? string.Empty
            : "An error occurred while changing email";
    }

    private async Task<string> HandlePasswordChange(User user, RedisUserUpdate userUpdate)
    {
        if (user is null
            || userUpdate is null
            || string.IsNullOrEmpty(userUpdate.NewValue)
            || string.IsNullOrEmpty(userUpdate.Salt))
            return "An error occurred while changing password";

        user.Password = userUpdate.NewValue;
        user.Salt = userUpdate.Salt;
        return await _userDbRepo.UpdateAsync(user)
            ? string.Empty
            : "An error occurred while changing password";
    }

    private async Task<string> HandleUsernameChange(User user, RedisUserUpdate userUpdate)
    {
        if (user is null || string.IsNullOrEmpty(userUpdate?.NewValue))
            return "An error occurred while changing username";

        user.Username = userUpdate.NewValue;
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

    public async Task<bool> UserExistsByIdAsync(Guid id) =>
        await _userDbRepo.GetUserByIdAsync(id) is not null;

    public string ValidateUserIdentity(List<Claim> claims,
        Guid userId,
        UserRole? compareRole = null,
        Func<UserRole, UserRole, bool> comparison = null)
    {
        var userIdFromClaim = claims.GetUserId();
        var role = claims.GetUserRole();
        if (role is null || userIdFromClaim is null)
            return "Authorization failed due to an invalid or missing role in the provided token";

        if (comparison == null)
        {
            if (userIdFromClaim.Value != userId)
                return "You do not have permission to access other users' data";
        }
        else
        {
            if (compareRole == null)
                throw new ArgumentNullException(nameof(compareRole),
                    "compareRole cannot be null when comparison is provided.");

            if (userIdFromClaim.Value != userId && !comparison(role.Value, compareRole.Value))
                return "You do not have permission to access other users' data";
        }

        return string.Empty;
    }

    public async Task<OperationResult<User>> ValidateUserUpdateAsync(SuUserUpdateRequest updateData)
    {
        if (!string.IsNullOrWhiteSpace(updateData.Email) && await UserExistsByEmailAsync(updateData.Email))
            return new OperationResult<User>("Email is already taken");

        if (!string.IsNullOrWhiteSpace(updateData.Username) && await UserExistsByUsernameAsync(updateData.Username))
            return new OperationResult<User>("A user with this username exists");

        var user = await _userDbRepo.GetUserByIdAsync(updateData.Id);
        return user is null
            ? new OperationResult<User>("Invalid input data")
            : new OperationResult<User>(user);
    }

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
        if (updateRequest.Role != null) filledFieldsCount++;

        return filledFieldsCount == 1;
    }

    private static bool IsAnyFieldProvided(SuUserUpdateRequest updateRequest)
    {
        var filledFieldsCount = 0;

        if (!string.IsNullOrWhiteSpace(updateRequest.Username)) filledFieldsCount++;
        if (!string.IsNullOrWhiteSpace(updateRequest.Email)) filledFieldsCount++;
        if (!string.IsNullOrWhiteSpace(updateRequest.NewPassword)) filledFieldsCount++;
        if (updateRequest.Role != null) filledFieldsCount++;

        return filledFieldsCount > 0;
    }

    private async Task<bool> UserExistsByUsernameAsync(string username) =>
        await _userCacheRepo.UserExistsByUsernameAsync(username)
        || await _userDbRepo.GetUserByUsernameAsync(username) is not null;

    #endregion

    #region Common

    public RedisUserUpdate GeneratePasswordUpdateEntityAsync(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            return null;

        var salt = UserHelper.GenerateSalt();
        var hashedPassword = UserHelper.GetPasswordHash(newPassword, salt);

        return new RedisUserUpdate
        {
            Id = default,
            NewValue = hashedPassword,
            Salt = salt,
            ChangeType = TokenType.PasswordChange
        };
    }

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