using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Roles;

namespace IdentityCore.Models.Response;

public record UserResponse
{
    public Guid Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required UserRole Role { get; init; }
    public required Provider Provider { get; init; }
}