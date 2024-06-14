using Helpers;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository;
using IdentityCore.Models;
using IdentityCore.Models.Request;
using IdentityCore.Models.Response;

namespace IdentityCore.Managers;

public class UserManager
{
    private UserRepository UserRepo;

    public UserManager(UserRepository userRepo)
    {
        UserRepo = userRepo;
    } 

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
                    Password = password is null ? UserHelper.GeneratePassword(12) : password
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

        return await UserRepo.AddedRange(usersToAdd);
}

    public async Task<User> CreateUser(UserRequest userRequest)
    {
        var salt = UserHelper.GetSalt();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = userRequest.Username,
            Email = userRequest.Email,
            Salt = salt,
            Password = UserHelper.GetPasswordHash(userRequest.Password, salt)
        };

        return await UserRepo.CreateAsync(user);
    }

    public async Task<OperationResult<User>> UpdateUser(UserRequest userRequest, User user)
    {
        var result = new OperationResult<User>();
        if (!string.IsNullOrWhiteSpace(userRequest.Username))
        {
            if (await UserRepo.UserExistsByUsernameAsync(user.Username))
            {
                result.Success = false;
                result.ErrorMessage = "UserName is already taken.";
                return result;
            }

            user.Username = userRequest.Username;
        }

        if (!string.IsNullOrWhiteSpace(userRequest.Email))
        {
            if (await UserRepo.UserExistsByEmailAsync(user.Email))
            {
                result.Success = false;
                result.ErrorMessage = "Email is already taken.";
                return result;
            }

            user.Email = userRequest.Email;
        }

        if (!string.IsNullOrWhiteSpace(userRequest.Password))
        {
            user.Salt = UserHelper.GetSalt();
            user.Password = UserHelper.GetPasswordHash(userRequest.Password, user.Salt);
        }


        if (await UserRepo.UpdateAsync(user))
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
        
        return await UserRepo.DeleteAsync(user);
    }
}