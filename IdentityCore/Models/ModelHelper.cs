using Helpers;
using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.Models.Request;
using IdentityCore.Models.Response;

namespace IdentityCore.Models;

public static class ModelHelper
{
    #region Users

    public static UserResponse ToUserResponse(this User user) =>
        new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            Provider = user.Provider
        };

    public static RedisUserUpdate ToRedisUserUpdate(this User user) =>
        new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
        };

    public static RedisUserUpdate ToRedisUserUpdate(this UserUpdateRequest userUpdateData)
    {
        var redisUserUpdate = new RedisUserUpdate
        {
            Id = userUpdateData.Id,
            Username = userUpdateData.Username,
            Email = userUpdateData.Email
        };

        if (!string.IsNullOrWhiteSpace(userUpdateData.NewPassword))
        {
            redisUserUpdate.Salt = UserHelper.GenerateSalt();
            redisUserUpdate.Password = UserHelper.GetPasswordHash(userUpdateData.NewPassword, redisUserUpdate.Salt);
        }

        return redisUserUpdate;
    }

    #endregion
}