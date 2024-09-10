using IdentityCore.DAL.PostgreSQL.Models;
using IdentityCore.Models;
using IdentityCore.Models.Response;

namespace IdentityCore.Managers.Interfaces;

public interface IAuthenticationManager
{
    Task<OperationResult<LoginResponse>> CreateLoginTokensAsync(User user);
    Task<OperationResult<LoginResponse>> RefreshLoginTokensAsync(RefreshToken token);
    Task<string> LogoutAsync(Guid userId, string refreshToken);
}