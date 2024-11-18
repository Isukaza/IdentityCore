using Helpers;

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
        public static string Host { get; private set; }
        public static string Queue { get; private set; }
        public static int Port { get; private set; }
        public static string Username { get; private set; }
        public static string Password { get; private set; }

        public static void Initialize(IConfiguration configuration, bool isDevelopment)
        {
            Host = DataHelper.GetRequiredString(configuration[Keys.HostKey], Keys.HostKey);
            Queue = DataHelper.GetRequiredString(configuration[Keys.QueueKey], Keys.QueueKey);

            Port = DataHelper.GetRequiredInt(configuration[Keys.PortKey], Keys.PortKey, 1, 65535);

            var rawUsername = isDevelopment
                ? configuration[Keys.UsernameKey]
                : Environment.GetEnvironmentVariable("RABBITMQ_USER");
            Username = DataHelper.GetRequiredString(rawUsername, Keys.UsernameKey, 3);

            var rawPassword = isDevelopment
                ? configuration[Keys.PasswordKey]
                : Environment.GetEnvironmentVariable("RABBITMQ_PASS");
            Password = DataHelper.GetRequiredString(rawPassword, Keys.PasswordKey, 32);
        }
    }
}