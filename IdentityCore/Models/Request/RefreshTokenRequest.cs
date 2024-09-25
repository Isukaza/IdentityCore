using System.ComponentModel.DataAnnotations;
using Helpers.ValidationAttributes;

namespace IdentityCore.Models.Request;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "RefreshToken is required")]
    [ValidToken]
    public required string RefreshToken { get; init; }
}