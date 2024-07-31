using IdentityCore.DAL.Models;
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
            Email = user.Email
        };

    #endregion
}