using System.ComponentModel.DataAnnotations;

namespace Helpers.ValidationAttributes;

public class NotEmptyGuidAttribute(string errorMessage) : ValidationAttribute
{
    private new string ErrorMessage { get; set; } = errorMessage;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is Guid guidValue && guidValue == Guid.Empty)
            return new ValidationResult(ErrorMessage);

        return ValidationResult.Success;
    }
}