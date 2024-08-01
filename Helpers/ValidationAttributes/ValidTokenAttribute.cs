using System.ComponentModel.DataAnnotations;

namespace Helpers.ValidationAttributes;

public class ValidTokenAttribute(string errorMessage = "Invalid token format.") : ValidationAttribute
{
    private const int Sha512ByteSize = 64;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var token = value as string;
        if (string.IsNullOrEmpty(token))
            return new ValidationResult("Token is required.");

        return !IsTokenValid(token) ? new ValidationResult(errorMessage) : ValidationResult.Success;
    }

    private static bool IsTokenValid(string token)
    {
        var bufferSize = (int)Math.Ceiling(token.Length * 3.0 / 4.0);
        var buffer = new Span<byte>(new byte[bufferSize]);
        if (Convert.TryFromBase64String(token, buffer, out var bytes))
            return bytes == Sha512ByteSize;

        return false;
    }
}