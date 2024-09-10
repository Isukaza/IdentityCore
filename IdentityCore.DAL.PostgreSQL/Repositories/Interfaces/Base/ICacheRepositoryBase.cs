namespace IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.Base;

public interface ICacheRepositoryBase
{
    Task<T> GetAsync<T>(string key);

    bool Add<T>(string key, T value, TimeSpan ttl);

    Task<bool> UpdateAsync<T>(string key, T value, TimeSpan expiry);
    Task<bool> UpdateTtlAsync(string key, TimeSpan ttl);

    Task<bool> DeleteAsync(string key);

    Task<bool> KeyExistsAsync(string key);
}