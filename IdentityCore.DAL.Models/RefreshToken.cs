using System.ComponentModel.DataAnnotations;
using IdentityCore.DAL.Models.Base;

namespace IdentityCore.DAL.Models;

public record RefreshToken : BaseDbEntity
{
    [Required(ErrorMessage = "RefreshToken is required")]
    public required string RefToken { get; set; }

    [Required(ErrorMessage = "Expires is required")]
    public required DateTime Expires { get; set; }

    #region Relational

    public Guid UserId { get; set; }
    public User User { get; set; }

    #endregion
}