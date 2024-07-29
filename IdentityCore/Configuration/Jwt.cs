using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace IdentityCore.Configuration;

public static class Jwt
{
    private static class Keys
    {
        private const string GroupName = "JWT";
        public const string IssuerKey = GroupName + ":Issuer";
        public const string AudienceKey = GroupName + ":Audience";
        public const string ExpiresKey = GroupName + ":Expires";
        public const string KeyKey = GroupName + ":Key";
    }

    public static class Configs
    {
        public static readonly string Issuer;
        public static readonly string Audience;
        public static readonly TimeSpan Expires;
        public static readonly SymmetricSecurityKey Key;

        static Configs()
        {
            var configuration = ConfigBase.GetConfiguration();
            Issuer = configuration[Keys.IssuerKey];
            Audience = configuration[Keys.AudienceKey];
            Expires = TimeSpan.FromMinutes(int.TryParse(configuration[Keys.ExpiresKey], out var expires)
                    ? expires
                    : 2);
            Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration[Keys.KeyKey] ?? string.Empty));
        }
    }
}