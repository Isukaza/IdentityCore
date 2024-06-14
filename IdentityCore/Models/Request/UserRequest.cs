using System.ComponentModel.DataAnnotations;

namespace IdentityCore.Models.Request;

public record UserRequest
{
   public Guid Id { get; init; } = Guid.Empty;
    
   [Required(ErrorMessage = "Username is required")]
   [StringLength(16, ErrorMessage = "Must be between 4 and 16 characters", MinimumLength = 4)]
   public string Username { get; init; }
    
   [Required(ErrorMessage = "Email is required")]
   [EmailAddress(ErrorMessage = "Invalid Email Address")]
   public string Email { get; init; }
   
   [Required(ErrorMessage = "Password is required")]
   [StringLength(255, ErrorMessage = "Must be between 8 and 255 characters", MinimumLength = 8)]
   [DataType(DataType.Password)]
   public string Password { get; init; }
}