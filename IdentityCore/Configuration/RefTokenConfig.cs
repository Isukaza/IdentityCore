using Helpers;

namespace IdentityCore.Configuration;

public static class RefTokenConfig
{
    private static class Keys
    {
        private const string GroupName = "RefreshToken";
        public const string ExpiresKey = GroupName + ":Expires";
        public const string MaxSessionsKey = GroupName + ":MaxSessions";
    }

    public static class Values
    {
        public static TimeSpan Expires { get; private set; }
        public static int MaxSessions { get; private set; }

        public static void Initialize(IConfiguration configuration)
        {
            Expires = DataHelper.GetValidatedTimeSpan(configuration[Keys.ExpiresKey], Keys.ExpiresKey, 1, 180);
            MaxSessions = DataHelper.GetRequiredInt(configuration[Keys.MaxSessionsKey], Keys.MaxSessionsKey, 1, 15);
        }
    }
}