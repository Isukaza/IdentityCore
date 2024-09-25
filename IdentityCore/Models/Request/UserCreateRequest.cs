using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using IdentityCore.Configuration.Constants;
using IdentityCore.Models.Interface;

namespace IdentityCore.Models.Request;

public class UserCreateRequest : IUser
{
    [JsonIgnore]
    [Obsolete("The Id property is not used in the UserCreateRequest class.", true)]
    public Guid Id { get; init; } = Guid.Empty;

    [Required(ErrorMessage = "Username is required")]
    [StringLength(16, ErrorMessage = "Username must be between 4 and 16 characters long.", MinimumLength = 4)]
    public required string Username { get; init; }

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

    [Required(ErrorMessage = "Confirm password is required")]
    [DataType(DataType.Password)]
    [StringLength(
        ValidationConstants.PassMaxLength,
        ErrorMessage = ValidationConstants.PassErrorMessage,
        MinimumLength = ValidationConstants.PassMinLength)]
    [RegularExpression(ValidationConstants.PassRegex, ErrorMessage = ValidationConstants.PassFormatErrorMessage)]
    [Compare("Password", ErrorMessage = "Password and Confirmation Password must match.")]
    public string ConfirmPassword { get; init; }
}