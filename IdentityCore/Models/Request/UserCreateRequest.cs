using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using IdentityCore.Models.Interface;

namespace IdentityCore.Models.Request;

public class UserCreateRequest : IUser
{
   [JsonIgnore]
   [Obsolete("The Id property is not used in the UserCreateRequest class.", true)]
   public Guid Id { get; init; } = Guid.Empty;
    
   [Required(ErrorMessage = "Username is required")]
   [StringLength(16, ErrorMessage = "Must be between 4 and 16 characters", MinimumLength = 4)]
   public required string Username { get; init; }
    
   [Required(ErrorMessage = "Email is required")]
   [EmailAddress(ErrorMessage = "Invalid Email Address")]
   public required string Email { get; init; }
   
   [Required(ErrorMessage = "Password is required")]
   [StringLength(255, ErrorMessage = "Must be between 8 and 255 characters", MinimumLength = 8)]
   [DataType(DataType.Password)]
   public required string Password { get; init; }
}