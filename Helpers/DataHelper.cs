using System.Security.Cryptography;
using System.Text;

namespace Helpers;

public static class DataHelper
{
    private const string Chars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ123456789!@#$%^&*()-_";

    private const int Sha512ByteSize = 64;
    private const int MaxLength = 255;

    #region GET

    /// <summary>
    /// Retrieves the configuration file name based on the current environment mode.
    /// </summary>
    /// <param name="isDevelopment">A boolean flag indicating whether the environment is development. 
    /// If <c>true</c>, it returns the configuration file for development, otherwise, it returns the default configuration file for other environments.</param>
    /// <returns>A string representing the configuration file name. 
    /// For development environment, it returns "appsettings.Development.json", and for other environments, it returns "appsettings.json".</returns>
    /// <example>
    /// Example usage:
    /// <code>
    /// bool isDevelopment = true;
    /// string configFile = DataHelper.GetConfigurationFileForMode(isDevelopment);
    /// Console.WriteLine(configFile); // Outputs "appsettings.Development.json"
    /// </code>
    /// </example>
    public static string GetConfigurationFileForMode(bool isDevelopment) =>
        isDevelopment
            ? "appsettings.Development.json"
            : "appsettings.json";

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

    /// <summary>
    /// Validates and retrieves a required string setting, with optional length validation.
    /// </summary>
    /// <param name="valueFromConfiguration">The configuration value to validate.</param>
    /// <param name="settingName">A friendly name for the setting (used in exception messages).</param>
    /// <param name="minLength">Optional minimum length for the string.</param>
    /// <returns>The validated configuration value.</returns>
    /// <exception cref="ArgumentException">Thrown if the value is missing, invalid, or violates length constraints.</exception>
    public static string GetRequiredSetting(string? valueFromConfiguration, string settingName, int? minLength = null)
    {
        if (string.IsNullOrWhiteSpace(valueFromConfiguration))
            throw new ArgumentException($"{settingName} is missing");

        if (minLength.HasValue && valueFromConfiguration.Length < minLength.Value)
            throw new ArgumentException($"{settingName} is too short. Minimum length is {minLength.Value} characters.");

        if (valueFromConfiguration.Length > MaxLength)
            throw new ArgumentException($"{settingName} is too long. Maximum length is {MaxLength} characters.");

        return valueFromConfiguration;
    }

    /// <summary>
    /// Validates and converts a string configuration value to a TimeSpan, within specified bounds.
    /// </summary>
    /// <param name="valueFromConfiguration">The configuration value to validate.</param>
    /// <param name="settingName">A friendly name for the setting (used in exception messages).</param>
    /// <param name="min">The minimum allowed value in minutes.</param>
    /// <param name="max">The maximum allowed value in minutes.</param>
    /// <returns>A TimeSpan representing the validated value.</returns>
    /// <exception cref="ArgumentException">Thrown if the value is missing or out of bounds.</exception>
    public static TimeSpan GetValidatedTimeSpan(string? valueFromConfiguration, string settingName, int min, int max)
    {
        if (!int.TryParse(valueFromConfiguration, out var value))
            throw new ArgumentException($"{settingName} is invalid or missing");

        if (value < min || value > max)
            throw new ArgumentException($"{settingName} should be between {min} and {max} minutes");

        return TimeSpan.FromMinutes(value);
    }

    #endregion
}