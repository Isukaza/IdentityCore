using System.Security.Cryptography;
using System.Text;

namespace Helpers;

public static class DataHelper
{
    private const string Chars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ123456789!@#$%^&*()-_";

    public static string GetHashFromStrings(params string[] args)
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

        var str = new string(GenerateRandomBytes(length).Select(b => Chars[b % Chars.Length]).ToArray());

        if (string.IsNullOrEmpty(str))
            throw new InvalidOperationException("Generated string is empty, which indicates a logical error.");

        return str;
    }
}