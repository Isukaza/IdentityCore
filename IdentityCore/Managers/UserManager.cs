using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Models.enums;
using IdentityCore.DAL.Repository.Interfaces;
using IdentityCore.Managers.Interfaces;
using IdentityCore.Models;
using IdentityCore.Models.Request;
using IdentityCore.Models.Response;

namespace IdentityCore.Managers;

public class UserManager : IUserManager
{
    #region C-tor and fields

    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _refTokenRepo;
    private readonly IConfirmationTokenRepository _ctRepo;

    private readonly IRefreshTokenManager _refTokenManager;

    public UserManager(IUserRepository userRepo,
        IRefreshTokenRepository refTokenRepo,
        IConfirmationTokenRepository ctRepo,
        IRefreshTokenManager refTokenManager)
    {
        _userRepo = userRepo;
        _refTokenRepo = refTokenRepo;
        _ctRepo = ctRepo;

        _refTokenManager = refTokenManager;
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

    public async Task<User> GetRegUserFromRedisByIdAsync(Guid id) =>
        await _userRepo.GetRegUserFromRedisByIdAsync(id);

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

    public async Task<bool> DeleteUserAsync(User user)
    {
        return user is not null && await _userRepo.DeleteAsync(user);
    }

    #endregion

    #region Redis

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

    public async Task<bool> DeleteRegisteredUserFromRedisAsync(User user)
    {
        if (user is null)
            return false;

        return await _userRepo.DeleteRegUserFromRedisAsync(user);
    }

    #endregion

    #region Token Management

    private static string CreateJwt(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, "Admin")
        };

        var jwt = new JwtSecurityToken(
            issuer: Jwt.Configs.Issuer,
            audience: Jwt.Configs.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(Jwt.Configs.Expires),
            signingCredentials: new SigningCredentials(Jwt.Configs.Key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public async Task<OperationResult<LoginResponse>> CreateLoginTokensAsync(User user)
    {
        var refreshToken = _refTokenManager.CreateRefreshToken(user);
        if (!await _refTokenManager.AddTokenAsync(user, refreshToken))
            return new OperationResult<LoginResponse>("Error creating session");

        var loginResponse = new LoginResponse
        {
            UserId = user.Id,
            Bearer = CreateJwt(user),
            RefreshToken = refreshToken.RefToken
        };

        return new OperationResult<LoginResponse>(loginResponse);
    }

    public async Task<OperationResult<LoginResponse>> RefreshLoginTokensAsync(RefreshToken token)
    {
        var updatedToken = await _refTokenManager.UpdateTokenDbAsync(token);
        if (string.IsNullOrWhiteSpace(updatedToken))
            return new OperationResult<LoginResponse>("Invalid operation");

        var loginResponse = new LoginResponse
        {
            UserId = token.UserId,
            Bearer = CreateJwt(token.User),
            RefreshToken = updatedToken
        };

        return new OperationResult<LoginResponse>(loginResponse);
    }

    public async Task<string> LogoutAsync(Guid userId, string refreshToken)
    {
        var token = await _refTokenRepo.GetTokenByUserIdAsync(userId, refreshToken);
        if (token is null)
            return "The user was not found or was deleted";

        return await _refTokenRepo.DeleteAsync(token)
            ? string.Empty
            : "Error during deletion";
    }

    public TokenType DetermineConfirmationTokenType(UserUpdateRequest updateRequest)
    {
        if (!string.IsNullOrWhiteSpace(updateRequest.Username))
            return TokenType.UsernameChange;

        if (!string.IsNullOrWhiteSpace(updateRequest.NewPassword))
            return TokenType.PasswordChange;

        return !string.IsNullOrWhiteSpace(updateRequest.Email)
            ? TokenType.EmailChangeOld
            : TokenType.Unknown;
    }

    public async Task<string> ProcessUserTokenActionAsync(
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

    #endregion

    #region Validation

    public async Task<string> GenerateUniqueUsernameAsync(string username)
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

    public async Task<bool> IsUserUpdateInProgressAsync(Guid id) =>
        await _userRepo.IsUserUpdateInProgress(id);

    #endregion

    #region Update Handling

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

    #region Test Methods

    public static List<TestUserResponse> GenerateUsers(int count, string password = null)
    {
        if (count < 1)
            return [];

        return Enumerable.Range(0, count)
            .Select(_ =>
            {
                var username = UserHelper.GenerateUsername();

                var user = new TestUserResponse
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    Email = UserHelper.GenerateEmail(username),
                    Password = password ?? UserHelper.GeneratePassword(32)
                };

                return user;
            })
            .ToList();
    }

    public async Task<bool> AddTestUsersToTheDatabaseAsync(List<TestUserResponse> users)
    {
        if (users.Count == 0)
            return false;

        var usersToAdd = users.Select(user =>
        {
            var salt = UserHelper.GenerateSalt();
            return new User
            {
                Id = Guid.NewGuid(),
                Username = user.Username,
                Email = user.Email,
                Salt = salt,
                Password = UserHelper.GetPasswordHash(user.Password, salt),
                Provider = Provider.Local
            };
        });

        return await _userRepo.AddedRangeAsync(usersToAdd);
    }

    #endregion

    private static bool IsSingleFieldProvided(UserUpdateRequest updateRequest)
    {
        var filledFieldsCount = 0;

        if (!string.IsNullOrWhiteSpace(updateRequest.Username)) filledFieldsCount++;
        if (!string.IsNullOrWhiteSpace(updateRequest.Email)) filledFieldsCount++;
        if (!string.IsNullOrWhiteSpace(updateRequest.OldPassword)) filledFieldsCount++;

        return filledFieldsCount == 1;
    }
}