using IdentityCore.DAL.PostgreSQL.Models.enums;

namespace IdentityCore.Configuration;

public static class TokenConfig
{
    private static class Keys
    {
        private const string GroupName = "TokenConfig:TTL";
        public const string RegistrationConfirmationKey = GroupName + ":RegistrationConfirmation";
        public const string EmailChangeOldKey = GroupName + ":EmailChangeOld";
        public const string EmailChangeNewKey = GroupName + ":EmailChangeNew";
        public const string PasswordResetKey = GroupName + ":PasswordReset";
        public const string PasswordChangeKey = GroupName + ":PasswordChange";
        public const string UsernameChangeKey = GroupName + ":UsernameChange";
    }

    public static class Values
    {
        public static readonly TimeSpan RegistrationConfirmation;
        public static readonly TimeSpan EmailChangeOld;
        public static readonly TimeSpan EmailChangeNew;
        public static readonly TimeSpan PasswordReset;
        public static readonly TimeSpan PasswordChange;
        public static readonly TimeSpan UsernameChange;

        static Values()
        {
            var configuration = ConfigBase.GetConfiguration();

            RegistrationConfirmation = GetTimeSpan(configuration, Keys.RegistrationConfirmationKey);
            EmailChangeOld = GetTimeSpan(configuration, Keys.EmailChangeOldKey);
            EmailChangeNew = GetTimeSpan(configuration, Keys.EmailChangeNewKey);
            PasswordReset = GetTimeSpan(configuration, Keys.PasswordResetKey);
            PasswordChange = GetTimeSpan(configuration, Keys.PasswordChangeKey);
            UsernameChange = GetTimeSpan(configuration, Keys.UsernameChangeKey);
        }

        private static TimeSpan GetTimeSpan(IConfiguration configuration, string key)
        {
            return TimeSpan.TryParse(configuration[key], out var timespan)
                ? timespan
                : TimeSpan.FromMinutes(15);
        }
    }
    
    public static TimeSpan GetTtlForTokenType(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.RegistrationConfirmation => Values.RegistrationConfirmation,
            TokenType.EmailChangeOld => Values.EmailChangeOld,
            TokenType.EmailChangeNew => Values.EmailChangeNew,
            TokenType.PasswordReset => Values.PasswordReset,
            TokenType.PasswordChange => Values.PasswordChange,
            TokenType.UsernameChange => Values.UsernameChange,
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null)
        };
    }
}