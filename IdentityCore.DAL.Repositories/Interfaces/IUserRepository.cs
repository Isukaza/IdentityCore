using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository.Interfaces.Base;

namespace IdentityCore.DAL.Repository.Interfaces;

public interface IUserRepository : IDbRepositoryBase<User>
{
    Task<User> GetRegUserFromRedisByIdAsync(Guid id);
    Task<RedisUserUpdate> GetUpdateUserFromRedisByIdAsync(Guid id);
    Task<User> GetUserByIdAsync(Guid id);
    Task<User> GetUserByUsernameAsync(string username);
    Task<User> GetUserByEmailAsync(string email);

    bool AddRegUserToRedis(User user, TimeSpan ttl);
    bool AddToRedisUpdateRequest(RedisUserUpdate updateRequest, TokenType tokenType, TimeSpan ttl);
    Task<bool> AddedRangeAsync(IEnumerable<User> users);

    Task<bool> DeleteRegUserFromRedisAsync(User user);
    Task<bool> DeleteUserUpdateDataFromRedisAsync(RedisUserUpdate update, TokenType tokenType);

    Task<bool> UpdateTtlRegUserAsync(User user, TimeSpan ttl);
    Task<bool> UpdateTtlUserUpdateAsync(RedisUserUpdate updateRequest, TokenType tokenType, TimeSpan ttl);

    Task<bool> IsUserUpdateInProgress(Guid id);
    Task<bool> UserExistsByUsernameAsync(string username);
    Task<bool> UserExistsByEmailAsync(string email);
}