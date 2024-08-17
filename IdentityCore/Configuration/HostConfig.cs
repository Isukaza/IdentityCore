namespace IdentityCore.Configuration;

public static class HostConfig
{
    private static class Keys
    {
        private const string GroupName = "Host";
        public const string HostKey = GroupName + ":URL";
        public const string RegistrationConfirmationPathKey = GroupName + ":RegistrationConfirmationPath";
        public const string ConfirmationTokenPathKey = GroupName + ":ConfirmationTokenPath";
    }

    public static class Values
    {
        public static readonly string Host;
        public static readonly string RegistrationConfirmationPath;
        public static readonly string ConfirmationTokenPath;

        static Values()
        {
            var configuration = ConfigBase.GetConfiguration();
            Host = configuration[Keys.HostKey];
            RegistrationConfirmationPath = configuration[Keys.RegistrationConfirmationPathKey];
            ConfirmationTokenPath = configuration[Keys.ConfirmationTokenPathKey];
        }
    }
}