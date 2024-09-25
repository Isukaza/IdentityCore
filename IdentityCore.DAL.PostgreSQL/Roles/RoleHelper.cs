using System.Security.Claims;

namespace IdentityCore.DAL.PostgreSQL.Roles;

public static class RoleHelper
{
    public static Guid? GetUserId(this IEnumerable<Claim> claims)
    {
        var value = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(value, out var userGuid))
            return userGuid;

        return null;
    }

    public static UserRole? GetUserRole(this IEnumerable<Claim> claims)
    {
        var value = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        if (Enum.TryParse(value, out UserRole userRole))
            return userRole;

        return null;
    }
}