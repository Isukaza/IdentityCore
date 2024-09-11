using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL.Models;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces;
using IdentityCore.Managers.Interfaces;
using IdentityCore.Models;
using IdentityCore.Models.Request;

namespace IdentityCore.Managers;

public class UserManager : IUserManager
{
    #region C-tor and fields

    private readonly IUserRepository _userRepo;
    private readonly IConfirmationTokenRepository _ctRepo;


    public UserManager(IUserRepository userRepo, IConfirmationTokenRepository ctRepo)
    {
        _userRepo = userRepo;
        _ctRepo = ctRepo;
    }

    #endregion

    #region CRUD

    public async Task<User> GetUserByIdAsync(Guid id) =>
        await _userRepo.GetUserByIdAsync(id);

    public async Task<OperationResult<User>> GetUserSsoAsync(string email)
    {
        var user = await _userRepo.GetUserByEmailAsync(email);
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
            return await _userRepo.CreateAsync(user);
        }

        user.Salt = UserHelper.GenerateSalt();
        user.Password = UserHelper.GetPasswordHash(userData.Password, user.Salt);
        user.IsActive = false;

        return _userRepo.AddRegUserToRedis(user, TokenConfig.Values.RegistrationConfirmation) ? user : null;
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
        return await _userRepo.UpdateAsync(user);
    }

    public async Task<bool> DeleteUserAsync(User user) =>
        user is not null && await _userRepo.DeleteAsync(user);

    #endregion

    #region Redis

    public async Task<User> GetRegUserFromRedisByIdAsync(Guid id) =>
        await _userRepo.GetRegUserFromRedisByIdAsync(id);
    
    public async Task<RedisUserUpdate> GetUpdateUserFromRedisByIdAsync(Guid id) =>
        await _userRepo.GetUpdateUserFromRedisByIdAsync(id);

    public RedisUserUpdate SaveUserUpdateToRedis(UserUpdateRequest updateRequest, TokenType tokenType)
    {
        var redisUserUpdate = new RedisUserUpdate
        {
            Id = updateRequest.Id,
            Username = updateRequest.Username,
            Email = updateRequest.Email
        };

        if (tokenType is TokenType.PasswordChange)
        {
            redisUserUpdate.Salt = UserHelper.GenerateSalt();
            redisUserUpdate.Password = UserHelper.GetPasswordHash(updateRequest.NewPassword, redisUserUpdate.Salt);
        }

        var ttl = TokenConfig.GetTtlForTokenType(tokenType);
        return _userRepo.AddToRedisUpdateRequest(redisUserUpdate, tokenType, ttl) ? redisUserUpdate : null;
    }

    public async Task<bool> DeleteRegisteredUserFromRedisAsync(User user) =>
        user is not null && await _userRepo.DeleteRegUserFromRedisAsync(user);

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
        var isUserRemoved = await _userRepo.DeleteRegUserFromRedisAsync(user);
        if (!isTokenRemoved || !isUserRemoved)
            return "Activation error";

        user.IsActive = true;
        return await _userRepo.CreateAsync(user) is not null
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
        var isUserUpdated = await _userRepo.UpdateTtlUserUpdateAsync(userUpdate, token.TokenType, ttl);

        if (isTokenUpdated && isUserUpdated)
            return string.Empty;

        _ = await _ctRepo.DeleteFromRedisAsync(token);
        _ = await _userRepo.DeleteUserUpdateDataFromRedisAsync(userUpdate, token.TokenType);

        return "Error changing email";
    }

    private async Task<string> HandleEmailChangeNew(User user, RedisUserUpdate userUpdate, RedisConfirmationToken token)
    {
        var isTokenRemoved = await _ctRepo.DeleteFromRedisAsync(token);
        var isUserRemoved = await _userRepo.DeleteUserUpdateDataFromRedisAsync(userUpdate, token.TokenType);
        if (!isTokenRemoved || !isUserRemoved || string.IsNullOrEmpty(userUpdate.Email))
            return "An error occurred while changing email";

        user.Email = userUpdate.Email;
        return await _userRepo.UpdateAsync(user)
            ? string.Empty
            : "An error occurred while changing email";
    }

    private async Task<string> HandlePasswordChange(User user, RedisUserUpdate userUpdate, RedisConfirmationToken token)
    {
        var isTokenRemoved = await _ctRepo.DeleteFromRedisAsync(token);
        var isUserRemoved = await _userRepo.DeleteUserUpdateDataFromRedisAsync(userUpdate, token.TokenType);
        if (!isTokenRemoved
            || !isUserRemoved
            || string.IsNullOrEmpty(userUpdate.Password)
            || string.IsNullOrEmpty(userUpdate.Salt))
            return "An error occurred while changing password";

        user.Password = userUpdate.Password;
        user.Salt = userUpdate.Salt;
        return await _userRepo.UpdateAsync(user)
            ? string.Empty
            : "An error occurred while changing password";
    }

    private async Task<string> HandleUsernameChange(User user, RedisUserUpdate userUpdate, RedisConfirmationToken token)
    {
        var isTokenRemoved = await _ctRepo.DeleteFromRedisAsync(token);
        var isUserRemoved = await _userRepo.DeleteUserUpdateDataFromRedisAsync(userUpdate, token.TokenType);
        if (!isTokenRemoved || !isUserRemoved || string.IsNullOrEmpty(userUpdate.Username))
            return "An error occurred while changing username";

        user.Username = userUpdate.Username;
        return await _userRepo.UpdateAsync(user)
            ? string.Empty
            : "An error occurred while changing username";
    }

    #endregion

    #region Validation

    public async Task<bool> IsUserUpdateInProgressAsync(Guid id) =>
        await _userRepo.IsUserUpdateInProgress(id);

    private static bool IsSingleFieldProvided(UserUpdateRequest updateRequest)
    {
        var filledFieldsCount = 0;

        if (!string.IsNullOrWhiteSpace(updateRequest.Username)) filledFieldsCount++;
        if (!string.IsNullOrWhiteSpace(updateRequest.Email)) filledFieldsCount++;
        if (!string.IsNullOrWhiteSpace(updateRequest.OldPassword)) filledFieldsCount++;

        return filledFieldsCount == 1;
    }

    public async Task<bool> UserExistsByEmailAsync(string email) =>
        await _userRepo.UserExistsByEmailAsync(email);

    public async Task<OperationResult<User>> ValidateUserUpdateAsync(UserUpdateRequest updateData)
    {
        if (!IsSingleFieldProvided(updateData))
            return new OperationResult<User>("Only one field can be provided for update");

        if (!string.IsNullOrWhiteSpace(updateData.Email)
            && await _userRepo.UserExistsByEmailAsync(updateData.Email))
            return new OperationResult<User>("Email is already taken");

        if (!string.IsNullOrWhiteSpace(updateData.Username)
            && await _userRepo.UserExistsByUsernameAsync(updateData.Username))
            return new OperationResult<User>("A user with this username exists");

        var user = await _userRepo.GetUserByIdAsync(updateData.Id);
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

        var user = await _userRepo.GetUserByEmailAsync(loginRequest.Email);
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

        if (await _userRepo.UserExistsByEmailAsync(userCreateRequest.Email))
            return "Email is already taken";

        if (await _userRepo.UserExistsByUsernameAsync(userCreateRequest.Username))
            return "A user with this username exists";

        return string.Empty;
    }

    #endregion

    #region Common

    private async Task<string> GenerateUniqueUsernameAsync(string username)
    {
        username = username.Replace(" ", "_");
        if (!await _userRepo.UserExistsByUsernameAsync(username))
            return username;

        var random = new Random();
        do
        {
            var newUsername = $"{username}_{random.Next(0, 10000):D4}";
            if (!await _userRepo.UserExistsByUsernameAsync(newUsername))
                return newUsername;
        } while (true);
    }

    #endregion
}