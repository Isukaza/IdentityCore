using System.Security.Cryptography;
using System.Text;

namespace Helpers;

public static class UserHelper
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

    public static string GetPasswordHash(string password, string salt) =>
        DataHelper.GetHashFromStrings(password, salt);

    public static string GetToken(Guid id)
    {
        var timestamp = DateTime.UtcNow.ToString("o");
        var randomString = DataHelper.GenerateString();
        return DataHelper.GetHashFromStrings(timestamp, id.ToString(), randomString);
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