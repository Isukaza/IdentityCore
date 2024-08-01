using System.ComponentModel.DataAnnotations;
using Helpers.ValidationAttributes;

namespace IdentityCore.Models.Request;

public record LogoutRequest
{
    [Required(ErrorMessage = "Id is required")]
    [NotEmptyGuid("Incorrect ID")]
    public required Guid UserId { get; set; }

    [Required(ErrorMessage = "RefreshToken is required")]
    [ValidToken]
    public string RefreshToken { get; set; }
}