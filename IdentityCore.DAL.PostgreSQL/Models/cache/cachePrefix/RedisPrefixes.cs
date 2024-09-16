namespace IdentityCore.DAL.PostgreSQL.Models.cache.cachePrefix;

public static class RedisPrefixes
{
    public static class User
    {
        public const string Registration = "UR";
        public const string Update = "UU";
        public const string Name = "Username";
        public const string Email = "Email";
    }

    public static class ConfirmationToken
    {
        public const string Prefix = "CT";
    }
}