using System.Text.Json;
using IdentityCore.Configuration;
using IdentityCore.Managers.Interfaces;
using MessagePack;
using RabbitMQ.Client;
using RabbitMQ.Messaging;
using RabbitMQ.Messaging.Models;

namespace IdentityCore.Managers;

public class MessageSenderManager(IRabbitMqConnection connection) : IMessageSenderManager
{
    public async Task<string> SendMessageAsync(UserUpdateMessage userUpdateMessage)
    {
        try
        {
            var body = MessagePackSerializer.Serialize(userUpdateMessage);
            await connection.Channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: RabbitMqConfig.Values.Queue,
                body: body);
            Console.WriteLine($" [x] Sent {JsonSerializer.Serialize(userUpdateMessage)}");
            
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [x] {ex.Message}");
            return ex.Message;
        }
    }
}