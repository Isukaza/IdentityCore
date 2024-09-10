using Microsoft.Build.Framework;
using Helpers.ValidationAttributes;
using IdentityCore.DAL.PostgreSQL.Models.enums;

namespace IdentityCore.Models.Base;

public abstract record ConfirmTokenBase
{
    [Required]
    [ValidToken]
    public string Token { get; set; }

    [Required]
    public TokenType TokenType { get; set; }
}