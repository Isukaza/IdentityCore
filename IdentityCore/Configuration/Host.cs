namespace IdentityCore.Configuration;

public static class Host
{
    private static class Keys
    {
        private const string GroupName = "Host";
        public const string HostKey = GroupName + ":URL";
        public const string RegistrationConfirmationPathKey = GroupName + ":RegistrationConfirmationPath";
    }

    public static class Configs
    {
        public static readonly string Host;
        public static readonly string RegistrationConfirmationPath;
        
        static Configs()
        {
            var configuration = ConfigBase.GetConfiguration();
            Host = configuration[Keys.HostKey];
            RegistrationConfirmationPath = configuration[Keys.RegistrationConfirmationPathKey];
        }
    }
}