using System.ComponentModel.DataAnnotations;
using Helpers.ValidationAttributes;

namespace IdentityCore.Models.Request;

public class ResendConfirmationEmailRequest
{
    [Required(ErrorMessage = "ID is required")]
    [NotEmptyGuid("Incorrect ID")]
    public Guid UserId { get; init; }
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public required string Email { get; init; }
}