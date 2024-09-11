using IdentityCore.DAL.PostgreSQL.Models;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.Models;
using IdentityCore.Models.Request;

namespace IdentityCore.Managers.Interfaces;

public interface IUserManager
{
    Task<User> GetUserByIdAsync(Guid id);
    Task<OperationResult<User>> GetUserSsoAsync(string email);
    Task<User> CreateUserForRegistrationAsync(UserCreateRequest userData, Provider provider);
    Task<OperationResult<User>> CreateUserSsoAsync(string email, string name, Provider provider);
    Task<bool> DeleteUserAsync(User user);
    
    Task<User> GetRegUserFromRedisByIdAsync(Guid id);
    Task<RedisUserUpdate> GetUpdateUserFromRedisByIdAsync(Guid id);
    RedisUserUpdate SaveUserUpdateToRedis(UserUpdateRequest updateRequest, TokenType tokenType);
    Task<bool> DeleteRegisteredUserFromRedisAsync(User user);

    Task<string> ExecuteUserUpdateFromTokenAsync(User user, RedisUserUpdate userUpdate, RedisConfirmationToken token);
    
    Task<bool> IsUserUpdateInProgressAsync(Guid id);
    Task<bool> UserExistsByEmailAsync(string email);
    Task<OperationResult<User>> ValidateUserUpdateAsync(UserUpdateRequest updateData);
    Task<OperationResult<User>> ValidateLoginAsync(UserLoginRequest loginRequest);
    Task<string> ValidateRegistrationAsync(UserCreateRequest userCreateRequest);
}