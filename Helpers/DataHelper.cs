using System.Security.Cryptography;
using System.Text;

namespace Helpers;

public static class DataHelper
{
    private const string Chars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ123456789!@#$%^&*()-_";
    private const int Sha512ByteSize = 64;

    #region GET

    public static string GetHashFromStrings(params string[] args)
    {
        if (args == null || args.Length == 0)
            throw new ArgumentException("At least one string argument is required.", nameof(args));

        var combinedString = string.Join("", args.Where(arg => !string.IsNullOrWhiteSpace(arg)));

        if (string.IsNullOrEmpty(combinedString))
            throw new ArgumentException("All provided strings are empty or whitespace.", nameof(args));

        var hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(combinedString));
        return Convert.ToBase64String(hashBytes);
    }

    #endregion

    #region Generators

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

    #endregion

    #region Validators

    public static bool IsTokenValid(string token)
    {
        var bufferSize = (int)Math.Ceiling(token.Length * 3.0 / 4.0);
        var buffer = new Span<byte>(new byte[bufferSize]);
        if (Convert.TryFromBase64String(token, buffer, out var bytes))
            return bytes == Sha512ByteSize;

        return false;
    }

    #endregion
}