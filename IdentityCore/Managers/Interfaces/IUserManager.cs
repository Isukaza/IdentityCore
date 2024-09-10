using IdentityCore.DAL.PostgreSQL.Models;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.Models;
using IdentityCore.Models.Request;
using IdentityCore.Models.Response;

namespace IdentityCore.Managers.Interfaces;

public interface IUserManager
{
    Task<User> GetUserByIdAsync(Guid id);
    Task<OperationResult<User>> GetUserSsoAsync(string email);
    Task<User> GetRegUserFromRedisByIdAsync(Guid id);
    Task<User> CreateUserForRegistrationAsync(UserCreateRequest userData, Provider provider);
    Task<OperationResult<User>> CreateUserSsoAsync(string email, string name, Provider provider);
    Task<bool> DeleteUserAsync(User user);

    Task<RedisUserUpdate> GetUpdateUserFromRedisByIdAsync(Guid id);
    RedisUserUpdate SaveUserUpdateToRedis(UserUpdateRequest updateRequest, TokenType tokenType);
    Task<bool> DeleteRegisteredUserFromRedisAsync(User user);

    Task<OperationResult<LoginResponse>> CreateLoginTokensAsync(User user);
    Task<OperationResult<LoginResponse>> RefreshLoginTokensAsync(RefreshToken token);
    Task<string> LogoutAsync(Guid userId, string refreshToken);
    TokenType DetermineConfirmationTokenType(UserUpdateRequest updateRequest);
    Task<string> ProcessUserTokenActionAsync(User user, RedisUserUpdate userUpdate, RedisConfirmationToken token);


    Task<string> GenerateUniqueUsernameAsync(string username);
    Task<bool> UserExistsByEmailAsync(string email);
    Task<OperationResult<User>> ValidateUserUpdateAsync(UserUpdateRequest updateData);
    Task<OperationResult<User>> ValidateLoginAsync(UserLoginRequest loginRequest);
    Task<string> ValidateRegistrationAsync(UserCreateRequest userCreateRequest);
    Task<bool> IsUserUpdateInProgressAsync(Guid id);

    Task<bool> AddTestUsersToTheDatabaseAsync(List<TestUserResponse> users);
}