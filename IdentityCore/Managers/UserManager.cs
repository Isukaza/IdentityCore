using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository;
using IdentityCore.Models;
using IdentityCore.Models.Request;
using IdentityCore.Models.Response;

namespace IdentityCore.Managers;

public class UserManager
{
    #region C-tor and fields

    private readonly UserRepository _userRepo;
    private readonly RefreshTokenRepository _refTokenRepo;
    private readonly ConfirmationTokenRepository _ctRepo;
    private readonly ConfirmationTokenManager _ctManager;
    private readonly MailManager _mailManager;

    private readonly RefreshTokenManager _refTokenManager;

    public UserManager(UserRepository userRepo,
        RefreshTokenRepository refTokenRepo,
        ConfirmationTokenRepository ctRepo,
        RefreshTokenManager refTokenManager,
        ConfirmationTokenManager ctManager,
        MailManager mailManager)
    {
        _userRepo = userRepo;
        _refTokenRepo = refTokenRepo;
        _ctRepo = ctRepo;

        _mailManager = mailManager;
        _ctManager = ctManager;
        _refTokenManager = refTokenManager;
    }

    #endregion

    #region CRUD

    public async Task<User> GetUserByIdAsync(Guid id) =>
        await _userRepo.GetUserByIdAsync(id);

    public User CreateUserForRegistration(UserCreateRequest userCreateRequest)
    {
        var salt = UserHelper.GenerateSalt();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = userCreateRequest.Username,
            Email = userCreateRequest.Email,
            Salt = salt,
            Password = UserHelper.GetPasswordHash(userCreateRequest.Password, salt)
        };

        return _userRepo.AddToRedis(user, TokenConfig.Values.RegistrationConfirmation) ? user : null;
    }

    public async Task<bool> DeleteUserAsync(User user)
    {
        if (user is null)
            return false;

        return await _userRepo.DeleteAsync(user);
    }

    #endregion

    #region Redis

    public async Task<RedisUserUpdate> GetUpdateUserFromRedisByIdAsync(Guid id) =>
        await _userRepo.GetUpdateUserFromRedisByIdAsync(id);

    public RedisUserUpdate SaveUserUpdateToRedisAsync(UserUpdateRequest updateRequest, TokenType tokenType)
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

        return await _userRepo.DeleteRegisteredUserFromRedisAsync(user);
    }

    #endregion

    #region Token Management

    private static string CreateJwt(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email)
        };

        var jwt = new JwtSecurityToken(
            issuer: Jwt.Configs.Issuer,
            audience: Jwt.Configs.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(Jwt.Configs.Expires),
            signingCredentials: new SigningCredentials(Jwt.Configs.Key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public async Task<OperationResult<LoginResponse>> CreateLoginTokens(User user)
    {
        var refreshToken = RefreshTokenManager.CreateRefreshToken(user);
        if (!await _refTokenManager.AddToken(user, refreshToken))
            return new OperationResult<LoginResponse>("Error creating session");

        var loginResponse = new LoginResponse
        {
            UserId = user.Id,
            Bearer = CreateJwt(user),
            RefreshToken = refreshToken.RefToken
        };

        return new OperationResult<LoginResponse>(loginResponse);
    }

    public async Task<OperationResult<LoginResponse>> RefreshLoginTokens(RefreshToken token)
    {
        var updatedToken = await _refTokenManager.UpdateTokenDb(token);
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

    public async Task<string> Logout(Guid userId, string refreshToken)
    {
        var token = await _refTokenRepo.GetTokenByUserIdAsync(userId, refreshToken);
        if (token is null)
            return "The user was not found or was deleted";

        return await _refTokenRepo.DeleteAsync(token)
            ? string.Empty
            : "Error during deletion";
    }

    public static TokenType DetermineConfirmationTokenType(UserUpdateRequest updateRequest)
    {
        if (!string.IsNullOrWhiteSpace(updateRequest.Username))
            return TokenType.UsernameChange;

        if (!string.IsNullOrWhiteSpace(updateRequest.NewPassword))
            return TokenType.PasswordChange;

        return !string.IsNullOrWhiteSpace(updateRequest.Email)
            ? TokenType.EmailChangeOld
            : TokenType.Unknown;
    }

    public async Task<string> ProcessUserTokenAction(
        User user,
        RedisUserUpdate userUpdate,
        RedisConfirmationToken token)
    {
        return token.TokenType switch
        {
            TokenType.RegistrationConfirmation => await HandleRegistrationConfirmation(user, token),
            TokenType.EmailChangeOld => await HandleEmailChangeOld(user, userUpdate, token),
            TokenType.EmailChangeNew => await HandleEmailChangeNew(user, userUpdate, token),
            TokenType.PasswordChange => await HandlePasswordChange(user, userUpdate, token),
            TokenType.UsernameChange => await HandleUsernameChange(user, userUpdate, token),
            _ => throw new ArgumentException("Unknown TokenType")
        };
    }

    #endregion

    #region Validation

    public async Task<OperationResult<User>> ValidateUserUpdateAsync(UserUpdateRequest updateRequest)
    {
        if (!IsSingleFieldProvided(updateRequest))
            return new OperationResult<User>("Only one field can be provided for update");

        if (!string.IsNullOrWhiteSpace(updateRequest.Email)
            && await _userRepo.UserExistsByEmailAsync(updateRequest.Email))
            return new OperationResult<User>("Email is already taken");

        if (!string.IsNullOrWhiteSpace(updateRequest.Username)
            && await _userRepo.UserExistsByUsernameAsync(updateRequest.Username))
            return new OperationResult<User>("A user with this username exists");

        var user = await _userRepo.GetUserByIdAsync(updateRequest.Id);
        if (user is null)
            return new OperationResult<User>("Invalid input data");

        if (string.IsNullOrWhiteSpace(updateRequest.OldPassword))
            return new OperationResult<User>(user);

        var hashCurrentPassword = UserHelper.GetPasswordHash(updateRequest.OldPassword, user.Salt);
        return hashCurrentPassword != user.Password
            ? new OperationResult<User>("Invalid input data")
            : new OperationResult<User>(user);
    }

    public async Task<OperationResult<User>> ValidateLogin(UserLoginRequest loginRequest)
    {
        if (string.IsNullOrWhiteSpace(loginRequest.Email) || string.IsNullOrWhiteSpace(loginRequest.Password))
            return new OperationResult<User>("Email or password is invalid");

        var user = await _userRepo.GetUserByEmailAsync(loginRequest.Email);
        if (user == null)
            return new OperationResult<User>("Email or password is invalid");

        var userPasswordHash = UserHelper.GetPasswordHash(loginRequest.Password, user.Salt);

        return userPasswordHash.Equals(user.Password)
            ? new OperationResult<User>(user)
            : new OperationResult<User>("Email or password is invalid");
    }

    public async Task<string> ValidateRegistration(UserCreateRequest userCreateRequest)
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

    public async Task<bool> IsUserUpdateInProgress(Guid id) =>
        await _userRepo.IsUserUpdateInProgress(id);

    #endregion

    #region Update Handling

    private async Task<string> HandleRegistrationConfirmation(User user, RedisConfirmationToken token)
    {
        var isTokenRemoved = await _ctRepo.DeleteFromRedisAsync(token);
        var isUserRemoved = await _userRepo.DeleteRegisteredUserFromRedisAsync(user);
        if (!isTokenRemoved || !isUserRemoved)
            return "Activation error";

        user.IsActive = true;
        return await _userRepo.CreateAsync(user) is not null
            ? string.Empty
            : "Activation error";
    }

    private async Task<string> HandleEmailChangeOld(User user, RedisUserUpdate userUpdate, RedisConfirmationToken token)
    {
        var isTokenRemoved = await _ctRepo.DeleteFromRedisAsync(token);
        if (!isTokenRemoved)
            return "Error changing email";

        var cfmToken = _ctManager.CreateConfirmationToken(user.Id, TokenType.EmailChangeNew);
        var cfmLink = MailConfig.GetConfirmationLink(cfmToken.Value, cfmToken.TokenType);
        var sendMailError = await _mailManager.SendEmailAsync(
            MailConfig.Values.Mail,
            userUpdate.Email,
            cfmToken.TokenType,
            cfmLink,
            user,
            userUpdate);

        var isUpdateTtl = await _userRepo
            .UpdateTtlUserUpdateAsync(userUpdate, cfmToken.TokenType, TokenConfig.Values.EmailChangeNew);
        if (string.IsNullOrEmpty(sendMailError) && isUpdateTtl)
            return string.Empty;

        _ = await _ctRepo.DeleteFromRedisAsync(cfmToken);
        _ = await _userRepo.DeleteUserUpdateDataFromRedisAsync(userUpdate, cfmToken.TokenType);

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

    public async Task<bool> AddTestUsersToTheDatabase(List<TestUserResponse> users)
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
                Password = UserHelper.GetPasswordHash(user.Password, salt)
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