using System.ComponentModel.DataAnnotations;
using IdentityCore.Configuration.Constants;

namespace IdentityCore.Models.Request;

public class UserLoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [StringLength(
        ValidationConstants.PassMaxLength,
        ErrorMessage = ValidationConstants.PassErrorMessage,
        MinimumLength = ValidationConstants.PassMinLength)]
    [RegularExpression(ValidationConstants.PassRegex, ErrorMessage = ValidationConstants.PassFormatErrorMessage)]
    public required string Password { get; init; }
}