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
    /// Validates and retrieves a required relative path.
    /// </summary>
    /// <param name="pathFromConfiguration">The relative path to validate.</param>
    /// <param name="settingName">A friendly name for the setting (used in exception messages).</param>
    /// <returns>The validated relative path.</returns>
    /// <exception cref="ArgumentException">Thrown if the path is missing or invalid.</exception>
    public static string GetRequiredPath(string? pathFromConfiguration, string settingName)
    {
        if (string.IsNullOrWhiteSpace(pathFromConfiguration))
            throw new ArgumentException($"{settingName} is missing");

        if (!pathFromConfiguration.StartsWith('/'))
            throw new ArgumentException(
                $"{settingName} is invalid. It must be a valid relative path starting with '/'.");

        if (pathFromConfiguration.Contains(".."))
            throw new ArgumentException(
                $"{settingName} is invalid. The relative path cannot contain '..' to navigate outside the root.");

        if (pathFromConfiguration.Any(c => !Uri.IsWellFormedUriString(c.ToString(), UriKind.Relative)))
            throw new ArgumentException($"{settingName} is invalid. The relative path contains invalid characters.");

        return pathFromConfiguration;
    }

    /// <summary>
    /// Validates and retrieves a required string setting, with optional length validation.
    /// </summary>
    /// <param name="valueFromConfiguration">The configuration value to validate.</param>
    /// <param name="settingName">A friendly name for the setting (used in exception messages).</param>
    /// <param name="minLength">Optional minimum length for the string.</param>
    /// <returns>The validated configuration value.</returns>
    /// <exception cref="ArgumentException">Thrown if the value is missing, invalid, or violates length constraints.</exception>
    public static string GetRequiredString(string? valueFromConfiguration, string settingName, int? minLength = null)
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
    /// Validates and retrieves a required integer setting within a specified range.
    /// </summary>
    /// <param name="valueFromConfiguration">The string value to convert to an integer.</param>
    /// <param name="settingName">A friendly name for the setting (used in exception messages).</param>
    /// <param name="min">The minimum acceptable value for the setting.</param>
    /// <param name="max">The maximum acceptable value for the setting.</param>
    /// <returns>The validated integer value.</returns>
    /// <exception cref="ArgumentException">Thrown if the value is missing, invalid, or out of range.</exception>
    public static int GetRequiredInt(string? valueFromConfiguration, string settingName, int min, int max)
    {
        if (string.IsNullOrWhiteSpace(valueFromConfiguration))
            throw new ArgumentException($"{settingName} is missing");

        if (!int.TryParse(valueFromConfiguration, out var result))
            throw new ArgumentException($"{settingName} is invalid. It must be a valid integer.");

        if (result < min)
            throw new ArgumentException($"{settingName} is too low. The minimum value is {min}.");

        if (result > max)
            throw new ArgumentException($"{settingName} is too high. The maximum value is {max}.");

        return result;
    }

    /// <summary>
    /// Validates and converts a string configuration value to a TimeSpan, ensuring the value is within the specified bounds (in minutes).
    /// The configuration value can be either a plain integer representing minutes or a valid TimeSpan string.
    /// </summary>
    /// <param name="valueFromConfiguration">The configuration value to validate, which can either be a string representing minutes or a TimeSpan string.</param>
    /// <param name="settingName">A friendly name for the setting (used in exception messages).</param>
    /// <param name="min">The minimum allowed value in minutes. The input value will be validated against this minimum.</param>
    /// <param name="max">The maximum allowed value in minutes. The input value will be validated against this maximum.</param>
    /// <returns>A TimeSpan representing the validated value, which is within the specified range in minutes.</returns>
    /// <exception cref="ArgumentException">Thrown if the value is missing, invalid, or out of bounds (not within the specified range in minutes).</exception>
    public static TimeSpan GetValidatedTimeSpan(string? valueFromConfiguration, string settingName, int min, int max)
    {
        if (string.IsNullOrWhiteSpace(valueFromConfiguration))
            throw new ArgumentException($"{settingName} is missing");

        if (int.TryParse(valueFromConfiguration, out var value))
        {
            if (value < min || value > max)
                throw new ArgumentException($"{settingName} should be between {min} and {max} minutes");

            return TimeSpan.FromMinutes(value);
        }

        if (TimeSpan.TryParse(valueFromConfiguration, out var timeSpan))
        {
            var minutes = (int)Math.Ceiling(timeSpan.TotalMinutes);
            if (minutes < min || minutes > max)
                throw new ArgumentException($"{settingName} should be between {min} and {max} minutes");

            return timeSpan;
        }

        throw new ArgumentException($"{settingName} is invalid or missing");
    }

    /// <summary>
    /// Validates and retrieves a required URL setting.
    /// </summary>
    /// <param name="urlFromConfiguration">The URL value to validate.</param>
    /// <param name="settingName">A friendly name for the setting (used in exception messages).</param>
    /// <returns>The validated URL.</returns>
    /// <exception cref="ArgumentException">Thrown if the URL is missing or invalid.</exception>
    public static string GetRequiredUrl(string? urlFromConfiguration, string settingName)
    {
        if (string.IsNullOrWhiteSpace(urlFromConfiguration))
            throw new ArgumentException($"{settingName} is missing");

        var isValidUrl = Uri.TryCreate(urlFromConfiguration, UriKind.Absolute, out var uriResult)
                         && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

        if (!isValidUrl)
            throw new ArgumentException($"{settingName} is invalid. It must be a valid HTTP or HTTPS URL.");

        return urlFromConfiguration;
    }

    #endregion
}