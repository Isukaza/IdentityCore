using System.Net;
using Helpers;
using IdentityCore.DAL.PostgreSQL.Models.enums;

namespace IdentityCore.Configuration;

public static class MailConfig
{
    private static class Keys
    {
        private const string GroupName = "Mail";
        public const string MaxAttemptsConfirmationResendKey = GroupName + ":MaxAttemptsConfirmationResend";
        public const string NextAttemptAvailableAfterKey = GroupName + ":NextAttemptAvailableAfter";
        public const string MinIntervalBetweenAttemptsKey = GroupName + ":MinIntervalBetweenAttempts";
    }

    public static class Values
    {
        public static int MaxAttemptsConfirmationResend { get; private set; }
        public static TimeSpan NextAttemptAvailableAfter { get; private set; }
        public static TimeSpan MinIntervalBetweenAttempts { get; private set; }


        public static void Initialize(IConfiguration configuration)
        {
            MaxAttemptsConfirmationResend = DataHelper.GetRequiredInt(
                configuration[Keys.MaxAttemptsConfirmationResendKey],
                Keys.MaxAttemptsConfirmationResendKey,
                1,
                10);

            NextAttemptAvailableAfter = DataHelper.GetValidatedTimeSpan(
                configuration[Keys.NextAttemptAvailableAfterKey],
                Keys.NextAttemptAvailableAfterKey,
                1,
                1440);

            MinIntervalBetweenAttempts = DataHelper.GetValidatedTimeSpan(
                configuration[Keys.MinIntervalBetweenAttemptsKey],
                Keys.MinIntervalBetweenAttemptsKey,
                1,
                1440);
        }
    }

    public static string GetConfirmationLink(string token, TokenType tokenType)
    {
        var tokenForUrl = WebUtility.UrlEncode(token);
        var cfmPath = tokenType == TokenType.RegistrationConfirmation
            ? HostConfig.Values.RegistrationConfirmationPath
            : HostConfig.Values.ConfirmationTokenPath;
        return $"{HostConfig.Values.Host}{cfmPath}?token={tokenForUrl}&tokenType={tokenType}";
    }
}