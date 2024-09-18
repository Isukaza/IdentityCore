using System.ComponentModel.DataAnnotations;

namespace Helpers.ValidationAttributes;

public class ValidTokenAttribute(string errorMessage = "Invalid token format.") : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var token = value as string;
        if (string.IsNullOrEmpty(token))
            return new ValidationResult("Token is required.");

        return !DataHelper.IsTokenValid(token)
            ? new ValidationResult(errorMessage)
            : ValidationResult.Success;
    }
}