using System.ComponentModel.DataAnnotations;
using Helpers.ValidationAttributes;
using IdentityCore.DAL.PostgreSQL.Models.enums;

namespace IdentityCore.Models.Request;

public record CfmTokenRequest
{
    [Required]
    [ValidToken]
    public string Token { get; init; }

    [Required]
    public TokenType TokenType { get; init; }
}