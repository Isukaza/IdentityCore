using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository;
using IdentityCore.DAL.Repository.Base;
using IdentityCore.Models;
using IdentityCore.Models.Request;
using IdentityCore.Models.Response;

namespace IdentityCore.Managers;

public class UserManager
{
    #region C-tor and fields

    private readonly UserRepository _userRepo;
    private readonly RefreshTokenRepository _refreshTokenRepo;
    private readonly ConfirmationTokenRepository _ctRepo;
    
    private readonly RefreshTokenManager _refreshTokenManager;

    public UserManager(UserRepository userRepo,
        RefreshTokenRepository refreshTokenRepository,
        ConfirmationTokenRepository ctRepo,
        RefreshTokenManager refreshTokenManager)
    {
        _userRepo = userRepo;
        _refreshTokenRepo = refreshTokenRepository;
        _ctRepo = ctRepo;

        _refreshTokenManager = refreshTokenManager;
    }

    #endregion

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

    private static string CreateJwt(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
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

    public async Task<OperationResult<LoginResponse>> CreateLoginTokens(User user)
    {
        var refreshToken = RefreshTokenManager.CreateRefreshToken(user);
        if (!await _refreshTokenManager.AddToken(user, refreshToken))
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
        var updatedToken = await _refreshTokenManager.UpdateTokenDb(token);
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

    public async Task<OperationResult<User>> UpdateUser(UserUpdateRequest updateRequest, User user)
    {
        if (!string.IsNullOrWhiteSpace(updateRequest.Username))
        {
            if (await _userRepo.UserExistsByUsernameAsync(updateRequest.Username))
                return new OperationResult<User>("UserName is already taken");

            user.Username = updateRequest.Username;
        }

        if (!string.IsNullOrWhiteSpace(updateRequest.Email))
        {
            if (await _userRepo.UserExistsByEmailAsync(updateRequest.Email))
                return new OperationResult<User>("Email is already taken");

            user.Email = updateRequest.Email;
        }

        if (!string.IsNullOrWhiteSpace(updateRequest.Password))
        {
            user.Salt = UserHelper.GenerateSalt();
            user.Password = UserHelper.GetPasswordHash(updateRequest.Password, user.Salt);
        }

        if (await _userRepo.UpdateAsync(user))
            return new OperationResult<User>(user);

        return new OperationResult<User>("Error updating user");
    }

    public async Task<bool> DeleteUserFromRedisAsync(User user)
    {
        if (user is null)
            return false;
        
        return await _userRepo.DeleteUserFromRedisAsync(user);
    }
    
    public async Task<bool> DeleteUserAsync(User user)
    {
        if (user is null)
            return false;

        return await _userRepo.DeleteAsync(user);
    }

    public async Task<string> Logout(Guid userId, string refreshToken)
    {
        var token = await _refreshTokenRepo.GetTokenByUserIdAsync(userId, refreshToken);
        if (token is null)
            return "The user was not found or was deleted";

        return await _refreshTokenRepo.DeleteAsync(token)
            ? string.Empty
            : "Error during deletion";
    }

    #region Validation

    public async Task<string> ActivatedUser(User user, ConfirmationToken token)
    {
        var isTokenRemoved = await _ctRepo.DeleteFromRedisAsync(token);
        var isUserRemoved = await _userRepo.DeleteUserFromRedisAsync(user);

        if (!isTokenRemoved || !isUserRemoved)
            return "Activation error";

        user.IsActive = true;
        return await _userRepo.CreateAsync(user) is not null
            ? string.Empty
            : "Activation error";
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

    #endregion

    #region TestMetods

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
}