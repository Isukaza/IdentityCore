using System.Net;
using Amazon;

namespace IdentityCore.Configuration;

public static class Mail
{
    private static class Keys
    {
        private const string GroupName = "Mail";
        public const string MailKey = GroupName + ":Mail";
        public const string RegionKey = GroupName + ":Region";
        public const string AwsAccessKeyIdKey = GroupName + ":AwsAccessKeyId";
        public const string AwsSecretAccessKeyKey = GroupName + ":AwsSecretAccessKey";
        public const string MaxAttemptsConfirmationResendKey = GroupName + ":MaxAttemptsConfirmationResend";
        public const string NextAttemptAvailableAfterKey = GroupName + ":NextAttemptAvailableAfter";
        public const string MinIntervalBetweenAttemptsKey = GroupName + ":MinIntervalBetweenAttempts";
    }

    public static class Configs
    {
        public static readonly string Mail;
        public static readonly RegionEndpoint RegionEndpoint;
        public static readonly string AwsAccessKeyId;
        public static readonly string AwsSecretAccessKey;
        public static readonly int MaxAttemptsConfirmationResend;
        public static readonly TimeSpan NextAttemptAvailableAfter;
        public static readonly TimeSpan MinIntervalBetweenAttempts;

        static Configs()
        {
            var configuration = ConfigBase.GetConfiguration();
            Mail = configuration[Keys.MailKey];
            RegionEndpoint = RegionEndpoint.GetBySystemName(configuration[Keys.RegionKey]);
            AwsAccessKeyId = configuration[Keys.AwsAccessKeyIdKey];
            AwsSecretAccessKey = configuration[Keys.AwsSecretAccessKeyKey];
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

    public static class Const
    {
        public const string Subject = "Confirm Your Registration";

        public static string GetHtmlContent(string username, string confirmationLink) =>
            $"""
             <p>Dear {username},</p>
             <p>Thank you for registering at SkillForge! Please click the link below to confirm your email address and complete your registration:</p>
             <p><a href='{confirmationLink}'>Confirm your email</a></p>
             <p>If you did not register for an account, please ignore this email.</p>
             <p>Best regards,<br>The SkillForge Team</p>
             """;

        public static string GetConfirmationLink(string token)
        {
            var tokenForUrl = WebUtility.UrlEncode(token);
            return Host.Configs.Host + Host.Configs.RegistrationConfirmationPath + tokenForUrl;
        }
    }
}