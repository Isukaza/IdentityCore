using System.ComponentModel.DataAnnotations;
using IdentityCore.DAL.Models.Base;

namespace IdentityCore.DAL.Models;

public record RegistrationToken : BaseDbEntity
{
    [Required(ErrorMessage = "RegistrationToken is required")]
    public required string RegToken { get; set; }
    
    [Required(ErrorMessage = "Expires is required")]
    public required DateTime Expires { get; set; }
    
    #region Relational
        
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    #endregion
}