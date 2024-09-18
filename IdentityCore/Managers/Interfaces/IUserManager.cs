using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.Models;
using IdentityCore.Models.Request;

namespace IdentityCore.Managers.Interfaces;

public interface IUserManager
{
    Task<User> GetUserByIdAsync(Guid id);
    Task<T> GetUserByIdAsync<T>(string prefix, Guid id);
    Task<User> GetUserByTokenTypeAsync(Guid id, TokenType tokenType);
    Task<OperationResult<User>> GetUserSsoAsync(string email);

    RedisUserUpdate AddUserUpdateDataByTokenType(RedisUserUpdate userUpdateData, TokenType tokenType, TimeSpan ttl);
    Task<User> CreateUserForRegistrationAsync(UserCreateRequest userData, Provider provider);
    Task<OperationResult<User>> CreateUserSsoAsync(string email, string name, Provider provider);

    Task<bool> UpdateTtlUserUpdateByTokenTypeAsync(RedisUserUpdate userData, TokenType tokenType, TimeSpan ttl);

    Task<bool> DeleteUserAsync(User user);
    Task<bool> DeleteUserDataByTokenTypeAsync(Guid id, string username, string email, TokenType tokenType);

    Task<string> ExecuteUserUpdateFromTokenAsync(User user, RedisUserUpdate userUpdData, RedisConfirmationToken token);

    Task<bool> IsUserUpdateInProgressAsync(Guid id);
    Task<bool> UserExistsByEmailAsync(string email);
    Task<OperationResult<User>> ValidateUserUpdateAsync(UserUpdateRequest updateData);
    Task<OperationResult<User>> ValidateLoginAsync(UserLoginRequest loginRequest);
    Task<string> ValidateRegistrationAsync(UserCreateRequest userCreateRequest);
}