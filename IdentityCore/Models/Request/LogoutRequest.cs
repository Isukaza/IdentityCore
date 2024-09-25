using System.ComponentModel.DataAnnotations;
using Helpers.ValidationAttributes;

namespace IdentityCore.Models.Request;

public record LogoutRequest
{
    [Required(ErrorMessage = "RefreshToken is required")]
    [ValidToken]
    public string RefreshToken { get; init; }
}