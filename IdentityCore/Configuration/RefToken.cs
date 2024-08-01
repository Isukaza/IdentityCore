namespace IdentityCore.Configuration;

public static class RefToken
{
    private static class Keys
    {
        private const string GroupName = "RefreshToken";
        public const string ExpiresKey = GroupName + ":Expires";
        public const string MaxSessionsKey = GroupName + ":MaxSessions";
    }

    public static class Configs
    {
        public static readonly TimeSpan Expires;
        public static readonly int MaxSessions;

        static Configs()
        {
            var configuration = ConfigBase.GetConfiguration();
            Expires = TimeSpan.FromDays(int.TryParse(configuration[Keys.ExpiresKey], out var expires)
                ? expires
                : 7);
            MaxSessions = int.TryParse(configuration[Keys.MaxSessionsKey], out var maxSessions)
                ? maxSessions
                : 5;
        }
    }
}