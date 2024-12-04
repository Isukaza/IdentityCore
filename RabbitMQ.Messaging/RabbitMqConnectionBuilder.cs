using RabbitMQ.Client;

namespace RabbitMQ.Messaging;

public class RabbitMqConnectionBuilder
{
    private string _host = "localhost";
    private string _queue = "default";
    private string _username = "guest";
    private string _password = "guest";
    private int _port = 5672;

    public RabbitMqConnectionBuilder WithHost(string host)
    {
        _host = host;
        return this;
    }

    public RabbitMqConnectionBuilder WithQueue(string queue)
    {
        _queue = queue;
        return this;
    }

    public RabbitMqConnectionBuilder WithCredentials(string username, string password)
    {
        _username = username;
        _password = password;
        return this;
    }

    public RabbitMqConnectionBuilder WithPort(int port)
    {
        _port = port;
        return this;
    }

    public async Task<RabbitMqConnection> BuildAsync()
    {
        ValidateConfiguration();
        
        var factory = new ConnectionFactory
        {
            HostName = _host,
            UserName = _username,
            Password = _password,
            Port = _port,
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: _queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        return new RabbitMqConnection(connection, channel);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_host))
            throw new ArgumentException("Host must be specified.", nameof(_host));

        if (string.IsNullOrWhiteSpace(_queue))
            throw new ArgumentException("Queue name must be specified.", nameof(_queue));

        if (_port <= 0 || _port > 65535)
            throw new ArgumentException("Port must be a valid TCP port number (1-65535).", nameof(_port));

        if (string.IsNullOrWhiteSpace(_username))
            throw new ArgumentException("Username must be specified.", nameof(_username));

        if (string.IsNullOrWhiteSpace(_password))
            throw new ArgumentException("Password must be specified.", nameof(_password));
    }
}