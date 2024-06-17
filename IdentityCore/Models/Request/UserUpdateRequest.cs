using Helpers.ValidationAttributes;
using IdentityCore.Models.Interface;

namespace IdentityCore.Models.Request;

using System.ComponentModel.DataAnnotations;

public class UserUpdateRequest : IUser
{
    [Required(ErrorMessage = "ID is required")]
    [NotEmptyGuid("Incorrect ID")]
    public Guid Id { get; init; }

    [StringLength(16, ErrorMessage = "Must be between 4 and 16 characters", MinimumLength = 4)]
    public string? Username { get; init; }

    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public string? Email { get; init; }

    [StringLength(255, ErrorMessage = "Must be between 8 and 255 characters", MinimumLength = 12)]
    [DataType(DataType.Password)]
    public string? Password { get; init; }
}