using System.Security.Claims;

using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Roles;
using IdentityCore.Models;
using IdentityCore.Models.Interface;
using IdentityCore.Models.Request;

namespace IdentityCore.Managers.Interfaces;

public interface IUserManager
{
    Task<User> GetUserByIdAsync(Guid id);
    Task<T> GetUserByIdAsync<T>(string prefix, Guid id);
    Task<User> GetUserByEmailAsync(string email);
    Task<User> GetUserByTokenTypeAsync(Guid id, TokenType tokenType);
    Task<OperationResult<User>> GetUserSsoAsync(string email);

    bool AddUserUpdateDataToRedis(RedisUserUpdate userUpdate);
    Task<User> CreateUserForRegistrationAsync(UserCreateRequest userData, Provider provider);
    Task<OperationResult<User>> CreateUserSsoAsync(string email, string name, Provider provider);

    Task<bool> UpdateUser(User user, SuUserUpdateRequest updateData);
    RedisUserUpdate CreateUserUpdateEntity(IUserUpdate updateRequest, User user);
    Task<bool> UpdateTtlUserUpdateByTokenTypeAsync(Guid userId, string username, string email, TokenType tokenType);

    Task<bool> DeleteUserAsync(User user);
    Task<bool> DeleteUserDataByTokenTypeAsync(Guid id, string username, string email, TokenType tokenType);

    Task<string> ExecuteUserUpdateFromTokenAsync(User user, RedisUserUpdate userUpd, RedisConfirmationToken token);

    Task<bool> IsUserUpdateInProgressAsync(Guid id);
    Task<bool> UserExistsByEmailAsync(string email);
    Task<bool> UserExistsByIdAsync(Guid id);
    string ValidateUserIdentity(List<Claim> claims,
        Guid userId,
        UserRole? compareRole = null,
        Func<UserRole, UserRole, bool> comparison = null);
    Task<OperationResult<User>> ValidateUserUpdateAsync(SuUserUpdateRequest updateData);
    Task<OperationResult<User>> ValidateUserUpdateAsync(UserUpdateRequest updateData);
    Task<OperationResult<User>> ValidateLoginAsync(UserLoginRequest loginRequest);
    Task<string> ValidateRegistrationAsync(UserCreateRequest userCreateRequest);

    RedisUserUpdate GeneratePasswordUpdateEntityAsync(string newPassword);
}