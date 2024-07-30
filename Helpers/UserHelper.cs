using System.Security.Cryptography;
using System.Text;

namespace Helpers;

public class UserHelper
{
    #region Fields

    private static readonly string[] SampleUsernames =
    [
        "Alice", "Bob", "Charlie", "Dave", "Eve",
        "Agatha", "Bogdan", "Vasilisa", "Vladislav", "Galina",
        "Denis", "Elizaveta", "Efim", "Zlata", "Ivan",
        "Kirill", "Larisa", "Maxim", "Nadezhda", "Oleg",
        "Polina", "Roman", "Svetlana", "Timur", "Ulyana"
    ];

    private static readonly string[] SampleEmails =
    [
        "gmail.com",
        "yahoo.com",
        "outlook.com",
        "hotmail.com",
        "aol.com",
        "icloud.com",
        "mail.com",
        "yandex.com",
        "zoho.com",
        "protonmail.com"
    ];

    #endregion

    #region GetMethods

    public static string GetHashStrings(params string[] args)
    {
        if (args == null || args.Length == 0)
            throw new ArgumentException("At least one string argument is required.", nameof(args));

        var combinedString = new StringBuilder();

        foreach (var arg in args)
            if (!string.IsNullOrWhiteSpace(arg))
                combinedString.Append(arg);

        var hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(combinedString.ToString()));
        return Convert.ToBase64String(hashBytes);
    }
    
    public static string GetPasswordHash(string password, string salt)
    {
        var bytePass = Encoding.Unicode.GetBytes(password + salt);
        var hashBytes = SHA512.HashData(bytePass);
        return Convert.ToBase64String(hashBytes);
    }
    
    #endregion

    #region Generators

    public static string GenerateSalt(int length = 64)
    {
        var saltBytes = DataHelper.GenerateRandomBytes(length);
        return Convert.ToBase64String(saltBytes);
    }

    public static string GeneratePassword(int length = 12) =>
        DataHelper.GenerateString(length);

    public static string GenerateRefreshToken()
    {
        var tokenByte = DataHelper.GenerateRandomBytes();
        return Convert.ToBase64String(tokenByte);
    }

    public static string GenerateUsername()
    {
        var random = new Random();
        var index = random.Next(SampleUsernames.Length);
        var username = SampleUsernames[index] + random.Next(1000, 9999);
        return username;
    }

    public static string GenerateEmail(string username)
    {
        var random = new Random();
        var index = random.Next(SampleEmails.Length);
        var email = $"{username}@{SampleEmails[index]}";
        return email;
    }

    #endregion
}