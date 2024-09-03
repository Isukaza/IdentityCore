using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace IdentityCore.Tests.Helpers;

public static class TestDataHelper
{
    private const string Chars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ123456789!@#$%^&*()-_";

    public static string GeneratePassword()
    {
        const int length = 24;
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[length];
        rng.GetBytes(randomBytes);

        return new string(randomBytes.Select(b => Chars[b % Chars.Length]).ToArray());
    }
    
    public static string GetPasswordHash(string password, string salt)
    {
        var data = Encoding.UTF8.GetBytes(string.Concat(password, salt));
        var hashBytes = SHA512.HashData(data);
        return Convert.ToBase64String(hashBytes);
    }
    
    public static string GenerateSalt() =>
        GenerateRandomString();
    
    public static string GenerateRandomToken() =>
        GenerateRandomString();

    private static string GenerateRandomString(int length=64)
    {
        var randomBytes = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(randomBytes);
        
        var hashBytes = SHA512.HashData(randomBytes);
        return Convert.ToBase64String(hashBytes);
    }
}