using Helpers.ValidationAttributes;
using IdentityCore.Configuration.Constants;
using IdentityCore.DAL.PostgreSQL.Roles;
using IdentityCore.Models.Interface;

namespace IdentityCore.Models.Request;

using System.ComponentModel.DataAnnotations;

public class UserUpdateRequest : IUserUpdate
{
    [Required(ErrorMessage = "ID is required")]
    [NotEmptyGuid("Incorrect ID")]
    public Guid Id { get; init; }

    [StringLength(16, ErrorMessage = "Must be between 4 and 16 characters", MinimumLength = 4)]
    public string Username { get; init; }

    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public string Email { get; init; }

    public UserRole? Role { get; init; }

    [DataType(DataType.Password)]
    [StringLength(
        ValidationConstants.PassMaxLength,
        ErrorMessage = ValidationConstants.PassErrorMessage,
        MinimumLength = ValidationConstants.PassMinLength)]
    [RegularExpression(ValidationConstants.PassRegex, ErrorMessage = ValidationConstants.PassFormatErrorMessage)]
    [BothOrNone("NewPassword", "Both OldPassword and NewPassword must be provided if one of them is set.")]
    public string OldPassword { get; init; }

    [DataType(DataType.Password)]
    [StringLength(
        ValidationConstants.PassMaxLength,
        ErrorMessage = ValidationConstants.PassErrorMessage,
        MinimumLength = ValidationConstants.PassMinLength)]
    [RegularExpression(ValidationConstants.PassRegex, ErrorMessage = ValidationConstants.PassFormatErrorMessage)]
    [BothOrNone("OldPassword", "Both OldPassword and NewPassword must be provided if one of them is set.")]
    public string NewPassword { get; init; }

    [DataType(DataType.Password)]
    [StringLength(
        ValidationConstants.PassMaxLength,
        ErrorMessage = ValidationConstants.PassErrorMessage,
        MinimumLength = ValidationConstants.PassMinLength)]
    [RegularExpression(ValidationConstants.PassRegex, ErrorMessage = ValidationConstants.PassFormatErrorMessage)]
    [Compare("NewPassword", ErrorMessage = ValidationConstants.PassMismatchErrorMessage)]
    public string ConfirmNewPassword { get; init; }
}