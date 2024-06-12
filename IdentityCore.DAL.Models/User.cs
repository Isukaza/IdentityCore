using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using IdentityCore.DAL.Models.Base;

namespace IdentityCore.DAL.Models;

public record User : BaseDbEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [Required(ErrorMessage = "Username is required")]
    [StringLength(16, ErrorMessage = "Must be between 4 and 16 characters", MinimumLength = 4)]
    public string Username { get; init; }
    
    [Display(Name = "Email address")]
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public string Email { get; init; }

    public string Salt { get; init; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(255, ErrorMessage = "Must be between 8 and 255 characters", MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; init; }
}