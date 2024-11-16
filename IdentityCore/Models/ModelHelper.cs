using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.Models.Response;
using RabbitMQ.Messaging.Models;

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

    public static UserUpdateMessage ToUserUpdateMessage(this RedisUserUpdate redis, User user, string cfmLink)
    {
        var userUpdateMessage = new UserUpdateMessage
        {
            UserEmail = user.Email,
            UserName = user.Username,
            ChangeType = redis.ChangeType,
            ConfirmationLink = cfmLink
        };

        if (redis.ChangeType != TokenType.PasswordChange || redis.ChangeType == TokenType.PasswordReset)
        {
            userUpdateMessage.NewValue = redis.NewValue;
            userUpdateMessage.OldValue = redis.ChangeType switch
            {
                TokenType.EmailChangeOld or TokenType.EmailChangeNew => user.Email,
                TokenType.UsernameChange => user.Username,
                _ => null
            };
        }
        
        return userUpdateMessage;
    }
    
    public static RedisUserUpdate ToRedisUserUpdate(this User user)
    {
        var userUpdateMessage = new RedisUserUpdate
        {
            Id = user.Id,
            ChangeType = TokenType.RegistrationConfirmation
        };
        
        return userUpdateMessage;
    }
    
    public static UserUpdateMessage ToUserUpdateMessage(this User user, string cfmLink)
    {
        var userUpdateMessage = new UserUpdateMessage
        {
            UserEmail = user.Email,
            UserName = user.Username,
            ChangeType = TokenType.RegistrationConfirmation,
            ConfirmationLink = cfmLink
        };
        
        return userUpdateMessage;
    }

    #endregion
}