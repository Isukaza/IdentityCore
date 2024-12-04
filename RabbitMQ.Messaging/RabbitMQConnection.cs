using RabbitMQ.Client;

namespace RabbitMQ.Messaging;

public class RabbitMqConnection(IConnection connection, IChannel channel) : IRabbitMqConnection
{
    public IChannel Channel { get; } = channel;

    public async ValueTask DisposeAsync()
    {
        if (connection != null)
        {
            await connection.CloseAsync();
            await connection.DisposeAsync();
        }

        if (Channel != null)
        {
            await Channel.CloseAsync();
            await Channel.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}