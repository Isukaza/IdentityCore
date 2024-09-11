using System.ComponentModel.DataAnnotations;
using IdentityCore.DAL.PostgreSQL.Models.Base;

namespace IdentityCore.DAL.PostgreSQL.Models.db;

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