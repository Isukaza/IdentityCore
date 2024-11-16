using RabbitMQ.Client;

namespace RabbitMQ.Messaging;

public interface IRabbitMqConnection : IAsyncDisposable
{
    IChannel Channel { get; }
}