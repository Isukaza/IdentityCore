using IdentityCore.DAL.PostgreSQL.Models;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.Models;
using IdentityCore.Models.Request;

namespace IdentityCore.Managers.Interfaces;

public interface IUserManager
{
    Task<User> GetUserByIdAsync(Guid id);
    Task<OperationResult<User>> GetUserSsoAsync(string email);
    Task<User> GetRegUserFromRedisByIdAsync(Guid id);
    Task<RedisUserUpdate> GetUpdateUserFromRedisByIdAsync(Guid id);
    Task<User> CreateUserForRegistrationAsync(UserCreateRequest userData, Provider provider);
    Task<OperationResult<User>> CreateUserSsoAsync(string email, string name, Provider provider);
    RedisUserUpdate SaveUserUpdateToRedis(UserUpdateRequest updateRequest, TokenType tokenType);
    Task<bool> DeleteUserAsync(User user);
    Task<bool> DeleteRegisteredUserFromRedisAsync(User user);

    Task<string> ProcessUserTokenActionAsync(User user, RedisUserUpdate userUpdate, RedisConfirmationToken token);

    Task<bool> UserExistsByEmailAsync(string email);
    Task<bool> IsUserUpdateInProgressAsync(Guid id);
    Task<OperationResult<User>> ValidateUserUpdateAsync(UserUpdateRequest updateData);
    Task<OperationResult<User>> ValidateLoginAsync(UserLoginRequest loginRequest);
    Task<string> ValidateRegistrationAsync(UserCreateRequest userCreateRequest);
}