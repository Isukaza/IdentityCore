using Helpers;

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
        public static string Host { get; private set; }
        public static string RegistrationConfirmationPath { get; private set; }
        public static string ConfirmationTokenPath { get; private set; }


        public static void Initialize(IConfiguration configuration)
        {
            Host = DataHelper.GetRequiredUrl(
                configuration[Keys.HostKey],
                Keys.HostKey);

            RegistrationConfirmationPath = DataHelper.GetRequiredPath(
                configuration[Keys.RegistrationConfirmationPathKey],
                Keys.RegistrationConfirmationPathKey);

            ConfirmationTokenPath = DataHelper.GetRequiredPath(
                configuration[Keys.ConfirmationTokenPathKey],
                Keys.ConfirmationTokenPathKey);
        }
    }
}