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
    }

    public static class Configs
    {
        public static readonly string Mail;
        public static readonly RegionEndpoint RegionEndpoint;
        public static readonly string AwsAccessKeyId;
        public static readonly string AwsSecretAccessKey;

        static Configs()
        {
            var configuration = ConfigBase.GetConfiguration();
            Mail = configuration[Keys.MailKey];
            RegionEndpoint = RegionEndpoint.GetBySystemName(configuration[Keys.RegionKey]);
            AwsAccessKeyId = configuration[Keys.AwsAccessKeyIdKey];
            AwsSecretAccessKey = configuration[Keys.AwsSecretAccessKeyKey];
        }
    }

    public static class Const
    {
        public const string Subject = "Confirm Your Registration";

        public static string GetHtmlContent(string username, string confirmationLink)
        {
            return $"""
                    <p>Dear {username},</p>
                    <p>Thank you for registering at SkillForge! Please click the link below to confirm your email address and complete your registration:</p>
                    <p><a href='{confirmationLink}'>Confirm your email</a></p>
                    <p>If you did not register for an account, please ignore this email.</p>
                    <p>Best regards,<br>The SkillForge Team</p>
                    """;
        }

        public static string GetConfirmationLink(string token)
        {
            var tokenForUrl = WebUtility.UrlEncode(token);
            return Host.Configs.Host + Host.Configs.RegistrationConfirmationPath + tokenForUrl;
        }
    }
}