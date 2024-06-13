using System.Security.Cryptography;
using System.Text;

namespace Helpers;

public class UserHelper
{
    private const string Chars = 
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ123456789!@#$%^&*()-_";

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
        
    public static string GetSalt(int length = 32)
    {
        var saltBytes = new byte[length];
        RandomNumberGenerator.Fill(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    public static string GetPasswordHash(string password, string salt)
    {
        var bytePass = Encoding.Unicode.GetBytes(password + salt);
        var hashBytes = SHA512.HashData(bytePass);
        return Convert.ToBase64String(hashBytes);
    }

    public static string GeneratePassword(int length)
    {
        var randomBytes = new byte[length];
        RandomNumberGenerator.Fill(randomBytes);

        var passwordChars = randomBytes
            .Select(b => Chars[b % Chars.Length])
            .ToArray();

        return new string(passwordChars);
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
}