using System.ComponentModel.DataAnnotations;
using IdentityCore.DAL.Models.Base;

namespace IdentityCore.DAL.Models;

public record User : BaseDbEntity
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(16, ErrorMessage = "Must be between 4 and 16 characters", MinimumLength = 4)]
    public required string Username { get; init; }
    
    [Display(Name = "Email address")]
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public required string Email { get; init; }

    public required string Salt { get; init; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(255, ErrorMessage = "Must be between 8 and 255 characters", MinimumLength = 8)]
    [DataType(DataType.Password)]
    public required string Password { get; init; }
}