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
    private readonly UserRepository _userRepo;
    private readonly RefreshTokenManager _refreshTokenManager;

    public UserManager(UserRepository userRepo, RefreshTokenManager refreshTokenManager)
    {
        _userRepo = userRepo;
        _refreshTokenManager = refreshTokenManager;
    } 
    
    public async Task<User> CreateUser(UserCreateRequest userCreateRequest)
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

        return await _userRepo.CreateAsync(user);
    }
    
    public async Task<OperationResult<LoginResponse>> CreateLoginTokens(User user)
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
            expires: Jwt.Configs.Expires,
            signingCredentials: new SigningCredentials(Jwt.Configs.Key, SecurityAlgorithms.HmacSha256));

        var refreshToken = new RefreshToken
        {
            RefToken = UserHelper.GenerateRefreshToken(),
            Expires = Rt.Configs.Expires,
            User = user
        };

        if (!await _refreshTokenManager.AddToken(user, refreshToken))
            return new OperationResult<LoginResponse> { ErrorMessage = "Error creating session" };

        return new OperationResult<LoginResponse>
        {
            Data = new LoginResponse
            {
                Bearer = new JwtSecurityTokenHandler().WriteToken(jwt),
                RefreshToken = refreshToken.RefToken
            },
            Success = true
        };
    }

    public async Task<OperationResult<User>> UpdateUser(UserUpdateRequest updateRequest, User user)
    {
        var result = new OperationResult<User>();
        if (!string.IsNullOrWhiteSpace(updateRequest.Username))
        {
            if (await _userRepo.UserExistsByUsernameAsync(user.Username))
            {
                result.Success = false;
                result.ErrorMessage = "UserName is already taken.";
                return result;
            }

            user.Username = updateRequest.Username;
        }

        if (!string.IsNullOrWhiteSpace(updateRequest.Email))
        {
            if (await _userRepo.UserExistsByEmailAsync(user.Email))
            {
                result.Success = false;
                result.ErrorMessage = "Email is already taken.";
                return result;
            }

            user.Email = updateRequest.Email;
        }

        if (!string.IsNullOrWhiteSpace(updateRequest.Password))
        {
            user.Salt = UserHelper.GenerateSalt();
            user.Password = UserHelper.GetPasswordHash(updateRequest.Password, user.Salt);
        }
        
        if (await _userRepo.UpdateAsync(user))
        {
            result.Data = user;
            return result;
        }

        result.Success = false;
        result.ErrorMessage = "Error updating user";
        
        return result;
    }

    public async Task<bool> DeleteUserAsync(User user)
    {
        if (user is null)
            return false;
        
        return await _userRepo.DeleteAsync(user);
    }

    public async Task<OperationResult<User>> ValidateUser(UserLoginRequest loginRequest)
    {
        if (string.IsNullOrWhiteSpace(loginRequest.Email) || string.IsNullOrWhiteSpace(loginRequest.Password))
        {
            return new OperationResult<User>
            {
                Success = false,
                ErrorMessage = "Email or password is invalid."
            };
        }

        var user = await _userRepo.GetUserByEmailAsync(loginRequest.Email);
        if (user == null)
        {
            return new OperationResult<User>
            {
                Success = false,
                ErrorMessage = "Email or password is invalid."
            };
        }

        var userPasswordHash = UserHelper.GetPasswordHash(loginRequest.Password, user.Salt);
        if (userPasswordHash.Equals(user.Password))
            return new OperationResult<User>
            {
                Data = user,
                Success = true
            };
        
        return new OperationResult<User>
        {
            Success = false,
            ErrorMessage = "Email or password is invalid"
        };
    }
    
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

        return await _userRepo.AddedRange(usersToAdd);
    }

    #endregion
}