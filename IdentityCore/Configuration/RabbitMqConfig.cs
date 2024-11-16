namespace IdentityCore.Configuration;

public static class RabbitMqConfig
{
    private static class Keys
    {
        private const string GroupName = "RabbitMq";
        public const string HostKey = GroupName + ":Host";
        public const string QueueKey = GroupName + ":Queue";
        public const string PortKey = GroupName + ":Port";
        public const string UsernameKey = GroupName + ":Username";
        public const string PasswordKey = GroupName + ":Password";
    }

    public static class Values
    {
        public static readonly string Host;
        public static readonly string Queue;
        public static readonly int Port;
        public static readonly string Username;
        public static readonly string Password;

        static Values()
        {
            var configuration = ConfigBase.GetConfiguration();
            Host = configuration[Keys.HostKey];
            Queue = configuration[Keys.QueueKey];

            Port = int.TryParse(configuration[Keys.PortKey], out var port)
                ? port
                : 5672;

            Username = configuration[Keys.UsernameKey];
            Password = configuration[Keys.PasswordKey];
        }
    }
}