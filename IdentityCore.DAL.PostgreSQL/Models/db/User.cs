using System.ComponentModel.DataAnnotations;
using IdentityCore.DAL.PostgreSQL.Models.db.Base;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Roles;

namespace IdentityCore.DAL.PostgreSQL.Models.db;

public record User : BaseDbEntity
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(16, ErrorMessage = "Must be between 4 and 16 characters", MinimumLength = 4)]
    public required string Username { get; set; }

    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public required string Email { get; set; }

    [StringLength(255, ErrorMessage = "Must be between 12 and 255 characters", MinimumLength = 12)]
    [DataType(DataType.Password)]
    public required string? Password { get; set; }

    public required string? Salt { get; set; }

    [Required(ErrorMessage = "Role is required")]
    public required UserRole Role { get; set; }

    [Required(ErrorMessage = "Provider is required")]
    public Provider Provider { get; set; }

    public bool IsActive { get; set; }

    #region Relational

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new HashSet<RefreshToken>();

    #endregion
}