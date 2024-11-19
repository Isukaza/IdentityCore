using Helpers;

namespace IdentityCore.Configuration;

public static class GoogleConfig
{
    private static class Keys
    {
        private const string GroupName = "GoogleAuth";
        public const string ClientIdKey = GroupName + ":ClientId";
        public const string ClientSecretKey = GroupName + ":ClientSecret";
        public const string RedirectUriKey = GroupName + ":RedirectUri";
        public const string ScopeKey = GroupName + ":Scope";
    }

    public static class Values
    {
        public static string ClientId { get; private set; }
        public static string ClientSecret { get; private set; }
        public static string RedirectUri { get; private set; }
        public static string Scope { get; private set; }


        public static void Initialize(IConfiguration configuration, bool isDevelopment)
        {
            ClientId = DataHelper.GetRequiredString(configuration[Keys.ClientIdKey], Keys.ClientIdKey);
            RedirectUri = DataHelper.GetRequiredString(configuration[Keys.RedirectUriKey], Keys.RedirectUriKey);
            Scope = DataHelper.GetRequiredString(configuration[Keys.ScopeKey], Keys.ScopeKey);
            
            var rawClientSecret = isDevelopment
                ? configuration[Keys.ClientSecretKey]
                : Environment.GetEnvironmentVariable("GOOGLE_AUTH_ClientSecret");
            
            ClientSecret = DataHelper.GetRequiredString(rawClientSecret, Keys.ClientSecretKey);
        }
    }
}