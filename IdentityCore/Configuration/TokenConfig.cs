using Helpers;
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
        public static TimeSpan RegistrationConfirmation { get; private set; }
        public static TimeSpan EmailChangeOld { get; private set; }
        public static TimeSpan EmailChangeNew { get; private set; }
        public static TimeSpan PasswordReset { get; private set; }
        public static TimeSpan PasswordChange { get; private set; }
        public static TimeSpan UsernameChange { get; private set; }

        public static void Initialize(IConfiguration configuration)
        {
            RegistrationConfirmation = GetTimeSpan(configuration, Keys.RegistrationConfirmationKey);
            EmailChangeOld = GetTimeSpan(configuration, Keys.EmailChangeOldKey);
            EmailChangeNew = GetTimeSpan(configuration, Keys.EmailChangeNewKey);
            PasswordReset = GetTimeSpan(configuration, Keys.PasswordResetKey);
            PasswordChange = GetTimeSpan(configuration, Keys.PasswordChangeKey);
            UsernameChange = GetTimeSpan(configuration, Keys.UsernameChangeKey);
        }

        private static TimeSpan GetTimeSpan(IConfiguration configuration, string key) =>
            DataHelper.GetValidatedTimeSpan(configuration[key], key, 1, 1440);
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