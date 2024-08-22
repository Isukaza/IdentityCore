using System.ComponentModel.DataAnnotations;

namespace Helpers.ValidationAttributes;

public class BothOrNoneAttribute : ValidationAttribute
{
    private readonly string _otherProperty;

    public BothOrNoneAttribute(string otherProperty, string errorMessage)
    {
        _otherProperty = otherProperty;
        ErrorMessage = errorMessage;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var instance = validationContext.ObjectInstance;
        var type = instance.GetType();

        var otherValue = type.GetProperty(_otherProperty)?.GetValue(instance) as string;
        var thisValue = value as string;

        if (!string.IsNullOrEmpty(thisValue) && string.IsNullOrEmpty(otherValue) ||
            !string.IsNullOrEmpty(otherValue) && string.IsNullOrEmpty(thisValue))
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}