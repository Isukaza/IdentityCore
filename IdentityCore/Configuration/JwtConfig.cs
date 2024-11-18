using System.Text;
using Microsoft.IdentityModel.Tokens;
using Helpers;

namespace IdentityCore.Configuration;

public static class JwtConfig
{
    private static class Keys
    {
        private const string GroupName = "JWT";
        public const string IssuerKey = GroupName + ":Issuer";
        public const string AudienceKey = GroupName + ":Audience";
        public const string ExpiresKey = GroupName + ":Expires";
        public const string KeyKey = GroupName + ":Key";
    }

    public static class Values
    {
        public static string Issuer { get; private set; }
        public static string Audience { get; private set; }
        public static TimeSpan Expires { get; private set; }
        public static SymmetricSecurityKey SymmetricSecurityKey { get; private set; }
        public static SigningCredentials SigningCredentials { get; private set; }

        public static void Initialize(IConfiguration configuration, bool isDevelopment)
        {
            Issuer = DataHelper.GetRequiredSetting(configuration[Keys.IssuerKey], Keys.IssuerKey);
            Audience = DataHelper.GetRequiredSetting(configuration[Keys.AudienceKey], Keys.AudienceKey);
            
            Expires = DataHelper.GetValidatedTimeSpan(configuration[Keys.ExpiresKey], Keys.ExpiresKey, 1, 120);

            var rawJwtKey = isDevelopment
                ? configuration[Keys.KeyKey]
                : Environment.GetEnvironmentVariable("JWT_KEY");

            var key = DataHelper.GetRequiredSetting(rawJwtKey, Keys.KeyKey, 32);

            SymmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            SigningCredentials = new SigningCredentials(SymmetricSecurityKey, SecurityAlgorithms.HmacSha256);
        }
    }
}