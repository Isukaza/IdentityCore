using System.ComponentModel.DataAnnotations;
using Helpers.ValidationAttributes;

namespace IdentityCore.Models.Request;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "UserId is required")]
    [NotEmptyGuid("Incorrect ID")]
    public required Guid UserId { get; set; }

    [Required(ErrorMessage = "RefreshToken is required")]
    [ValidToken]
    public required string RefreshToken { get; set; }
}