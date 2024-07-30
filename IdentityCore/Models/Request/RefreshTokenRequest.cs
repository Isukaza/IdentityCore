using System.ComponentModel.DataAnnotations;

namespace IdentityCore.Models.Request;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(16, ErrorMessage = "Must be between 4 and 16 characters", MinimumLength = 4)]
    public required string Username { get; set; }

    [Required(ErrorMessage = "RefreshToken is required")]
    public required string RefreshToken { get; set; }
}