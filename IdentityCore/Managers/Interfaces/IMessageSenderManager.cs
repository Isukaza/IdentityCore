using RabbitMQ.Messaging.Models;

namespace IdentityCore.Managers.Interfaces;

public interface IMessageSenderManager
{
    Task<string> SendMessageAsync(UserUpdateMessage userUpdateMessage);
}