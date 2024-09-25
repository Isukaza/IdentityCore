using IdentityCore.DAL.PostgreSQL.Roles;

namespace IdentityCore.Models.Interface;

public interface IUserUpdate
{
    Guid Id { get; init; }

    string Username { get; init; }

    string Email { get; init; }

    UserRole? Role { get; init; }
    
    public string NewPassword { get; init; }
    
    public string ConfirmNewPassword { get; init; }
}