using Helpers.ValidationAttributes;

namespace IdentityCore.Models.Request;

using System.ComponentModel.DataAnnotations;

public class UserUpdateRequest
{
    [Required(ErrorMessage = "ID is required")]
    [NotEmptyGuid("Incorrect ID")]
    public Guid Id { get; init; }

    [StringLength(16, ErrorMessage = "Must be between 4 and 16 characters", MinimumLength = 4)]
    public string Username { get; init; }

    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public string Email { get; init; }

    [DataType(DataType.Password)]
    [StringLength(255, ErrorMessage = "The password must be between 12 and 255 characters long.", MinimumLength = 12)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$", ErrorMessage = "The current password does not match the format")]
    [BothOrNone("NewPassword", "Both OldPassword and NewPassword must be provided if one of them is set.")]
    public string OldPassword { get; init; }

    [DataType(DataType.Password)]
    [StringLength(255, ErrorMessage = "The password must be between 12 and 255 characters long.", MinimumLength = 12)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$", ErrorMessage = "The new password does not match the format")]
    [BothOrNone("OldPassword", "Both OldPassword and NewPassword must be provided if one of them is set.")]
    public string NewPassword { get; init; }

    [DataType(DataType.Password)]
    [StringLength(255, ErrorMessage = "The password must be between 12 and 255 characters long.", MinimumLength = 12)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$", ErrorMessage = "Confirm new password does not match format")]
    [Compare("NewPassword", ErrorMessage = "NewPassword and ConfirmNewPassword must match.")]
    public string ConfirmNewPassword { get; init; }
}