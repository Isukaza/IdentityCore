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
        public static readonly string ClientId;
        public static readonly string ClientSecret;
        public static readonly string RedirectUri;
        public static readonly string Scope;

        static Values()
        {
            var configuration = ConfigBase.GetConfiguration();
            ClientId = configuration[Keys.ClientIdKey];
            ClientSecret = configuration[Keys.ClientSecretKey];
            RedirectUri = configuration[Keys.RedirectUriKey];
            Scope = configuration[Keys.ScopeKey];
        }
    }
}