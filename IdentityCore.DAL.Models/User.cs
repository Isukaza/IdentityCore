using System.ComponentModel.DataAnnotations;
using IdentityCore.DAL.Models.Base;

namespace IdentityCore.DAL.Models;

public record User : BaseDbEntity
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(16, ErrorMessage = "Must be between 4 and 16 characters", MinimumLength = 4)]
    public required string Username { get; set; }

    [Display(Name = "Email address")]
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(255, ErrorMessage = "Must be between 12 and 255 characters", MinimumLength = 12)]
    [DataType(DataType.Password)]
    public required string Password { get; set; }

    public required string Salt { get; set; }

    public bool IsActive { get; set; }
    
    #region Relational

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new HashSet<RefreshToken>();
    
    #endregion
}