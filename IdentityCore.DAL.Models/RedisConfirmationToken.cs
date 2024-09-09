﻿using System.ComponentModel.DataAnnotations;
using IdentityCore.DAL.Models.enums;

namespace IdentityCore.DAL.Models;

public record RedisConfirmationToken
{
    public Guid UserId { get; init; }

    [Required(ErrorMessage = "Token is required")]
    public required string Value { get; set; }
    
    [Required(ErrorMessage = "TokenType is required")]
    public required TokenType TokenType { get; set; }

    public int AttemptCount { get; set; } = 1;
    
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}