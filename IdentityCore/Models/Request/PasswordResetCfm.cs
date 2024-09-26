using System.ComponentModel.DataAnnotations;
using Helpers.ValidationAttributes;
using IdentityCore.Configuration.Constants;

namespace IdentityCore.Models.Request;

public record class PasswordResetCfm
{
    [Required]
    [ValidToken]
    public required string Token { get; init; }
    
    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [StringLength(
        ValidationConstants.PassMaxLength,
        ErrorMessage = ValidationConstants.PassErrorMessage,
        MinimumLength = ValidationConstants.PassMinLength)]
    [RegularExpression(ValidationConstants.PassRegex, ErrorMessage = ValidationConstants.PassFormatErrorMessage)]
    public required string Password { get; init; }
    
    [Required(ErrorMessage = "Confirm password is required")]
    [DataType(DataType.Password)]
    [StringLength(
        ValidationConstants.PassMaxLength,
        ErrorMessage = ValidationConstants.PassErrorMessage,
        MinimumLength = ValidationConstants.PassMinLength)]
    [RegularExpression(ValidationConstants.PassRegex, ErrorMessage = ValidationConstants.PassFormatErrorMessage)]
    [Compare("Password", ErrorMessage = "Password and Confirmation Password must match.")]
    public required string ConfirmPassword { get; init; }
}