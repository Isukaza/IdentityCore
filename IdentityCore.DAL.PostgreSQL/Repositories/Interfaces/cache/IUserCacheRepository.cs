using IdentityCore.DAL.PostgreSQL.Models.db;

namespace IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.cache;

public interface IUserCacheRepository
{
    Task<T> GetUserByIdAsync<T>(string prefix, Guid id);
    
    bool AddMappingToCache(string keyPrefix, string value, TimeSpan ttl);
    bool AddEntityToCache<T>(string keyPrefix, Guid id, T entity, TimeSpan ttl);

    Task<bool> UpdateTtlEntityInCache(string keyPrefix, string id, TimeSpan ttl);
    
    Task<bool> RemoveEntityFromCache(string keyPrefix, string value);
    
    Task<bool> IsUserUpdateInProgressAsync(Guid id);
    Task<bool> UserExistsByEmailAsync(string email);
    Task<bool> UserExistsByUsernameAsync(string username);
}