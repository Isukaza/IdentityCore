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
            Provider = user.Provider
        };

    public static RedisUserUpdate ToRedisUserUpdate(this UserUpdateRequest userUpdateData) =>
        new()
        {
            Id = userUpdateData.Id,
            Username = userUpdateData.Username,
            Email = userUpdateData.Email
        };

    #endregion
}