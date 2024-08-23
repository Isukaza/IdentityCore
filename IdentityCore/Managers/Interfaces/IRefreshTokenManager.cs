using IdentityCore.DAL.Models;
using IdentityCore.Models;

namespace IdentityCore.Managers.Interfaces;

public interface IRefreshTokenManager
{
    RefreshToken CreateRefreshToken(User user);

    Task<bool> AddTokenAsync(User user, RefreshToken refreshToken);

    Task<string> UpdateTokenDbAsync(RefreshToken token);

    Task<OperationResult<RefreshToken>> ValidationRefreshTokenAsync(Guid userId, string token);
}