using System.ComponentModel.DataAnnotations;

namespace IdentityCore.Models.Request;

public class UserLoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(255, ErrorMessage = "Must be between 12 and 255 characters", MinimumLength = 12)]
    [DataType(DataType.Password)]
    public required string Password { get; init; }
}