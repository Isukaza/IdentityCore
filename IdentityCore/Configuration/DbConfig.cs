using Helpers;

namespace IdentityCore.Configuration;

public static class DbConfig
{
    /// <summary>
    /// Retrieves and validates the PostgreSQL username from environment variables.
    /// </summary>
    public static string GetPostgreSqlUsernameFromEnv()
    {
        var username = Environment.GetEnvironmentVariable("POSTGRES_USER_IC");
        return DataHelper.GetRequiredSetting(username, "ENV POSTGRES_USER_IC", 3);
    }

    /// <summary>
    /// Retrieves and validates the PostgreSQL password from environment variables.
    /// </summary>
    public static string GetPostgreSqlPasswordFromEnv()
    {
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD_IC");
        return DataHelper.GetRequiredSetting(password, "ENV POSTGRES_PASSWORD_IC", 32);
    }
}