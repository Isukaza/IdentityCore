using Helpers;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository;
using IdentityCore.Models;
using IdentityCore.Models.Request;
using IdentityCore.Models.Response;

namespace IdentityCore.Managers;

public class UserManager
{
    private readonly UserRepository _userRepo;

    public UserManager(UserRepository userRepo)
    {
        _userRepo = userRepo;
    } 

    public static List<TestUserResponse> GenerateUsers(int count, string? password = null)
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
                    Password = password ?? UserHelper.GeneratePassword(12)
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
            var salt = UserHelper.GetSalt();
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

    public async Task<User> CreateUser(UserCreateRequest userCreateRequest)
    {
        var salt = UserHelper.GetSalt();
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
            user.Salt = UserHelper.GetSalt();
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
}