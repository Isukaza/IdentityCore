using System.Net;
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
        public static readonly int MaxAttemptsConfirmationResend;
        public static readonly TimeSpan NextAttemptAvailableAfter;
        public static readonly TimeSpan MinIntervalBetweenAttempts;

        static Values()
        {
            var configuration = ConfigBase.GetConfiguration();
            MaxAttemptsConfirmationResend =
                int.TryParse(configuration[Keys.MaxAttemptsConfirmationResendKey], out var maxAttempts)
                    ? maxAttempts
                    : 3;
            NextAttemptAvailableAfter =
                TimeSpan.TryParse(configuration[Keys.NextAttemptAvailableAfterKey], out var nextAttempt)
                    ? nextAttempt
                    : TimeSpan.FromMinutes(10);
            MinIntervalBetweenAttempts =
                TimeSpan.TryParse(configuration[Keys.MinIntervalBetweenAttemptsKey], out var minInterval)
                    ? minInterval
                    : TimeSpan.FromMinutes(1);
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