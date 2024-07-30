using System.Security.Cryptography;

namespace Helpers;

public static class DataHelper
{
    private const string Chars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ123456789!@#$%^&*()-_";

    public static byte[] GenerateRandomBytes(int length = 64)
    {
        if (length < 1)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be a positive number.");

        var randomBytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return randomBytes;
    }

    public static string GenerateString(int length = 64)
    {
        if (length < 1)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be a positive number.");

        var str = GenerateRandomBytes(length)
            .Select(b => Chars[b % Chars.Length])
            .ToString();

        if (string.IsNullOrEmpty(str))
            throw new InvalidOperationException("Generated string is empty, which indicates a logical error.");

        return str;
    }
}