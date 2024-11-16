using RabbitMQ.Client;

namespace RabbitMQ.Messaging;

public class RabbitMqConnection : IRabbitMqConnection
{
    private IConnection _connection;
    public IChannel Channel { get; private set; }

    public RabbitMqConnection(
        string host,
        string queue,
        string username,
        string password,
        int port)
    {
        InitializeConnection(host, queue, username, password, port)
            .GetAwaiter()
            .GetResult();
    }

    private async Task InitializeConnection(
        string host,
        string queue,
        string username,
        string password,
        int port)
    {
        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = username,
            Password = password,
            Port = port,
        };

        _connection = await factory.CreateConnectionAsync();
        Channel = await _connection.CreateChannelAsync();

        await Channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        if (Channel != null)
        {
            await Channel.CloseAsync();
            await Channel.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}