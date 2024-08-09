using System.ComponentModel.DataAnnotations;

namespace IdentityCore.DAL.Models;

public record ConfirmationToken
{
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Token is required")]
    public required string Value { get; set; }
    
    [Required(ErrorMessage = "TokenType is required")]
    public required TokenType TokenType { get; set; }

    public int AttemptCount { get; set; } = 1;
    
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}