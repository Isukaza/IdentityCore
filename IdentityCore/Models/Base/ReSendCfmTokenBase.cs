using System.ComponentModel.DataAnnotations;
using IdentityCore.DAL.PostgreSQL.Models.enums;

namespace IdentityCore.Models.Base;

public abstract record ReSendCfmTokenBase
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    public TokenType TokenType { get; init; }
}