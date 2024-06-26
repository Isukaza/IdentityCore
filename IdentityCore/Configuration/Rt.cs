namespace IdentityCore.Configuration;

public static class Rt
{
    private static class Keys
    {
        private const string GroupName = "RefreshToken";
        public const string ExpiresKey = GroupName + ":Expires";
        public const string MaxSessionsKey = GroupName + ":MaxSessions";
    }

    public static class Configs
    {
        public static readonly DateTime Expires;
        public static readonly int MaxSessions;

        static Configs()
        {
            var configuration = GetConfiguration();
            Expires = DateTime.UtcNow.Add(
                TimeSpan.FromDays(int.TryParse(configuration[Keys.ExpiresKey], out var expires)
                    ? expires
                    : 7));
            MaxSessions = int.TryParse(configuration[Keys.MaxSessionsKey], out var maxSessions)
                ? maxSessions
                : 5;
        }

        private static IConfigurationRoot GetConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }
    }
}