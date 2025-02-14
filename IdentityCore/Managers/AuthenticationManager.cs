using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces.db;
using IdentityCore.Managers.Interfaces;
using IdentityCore.Models;
using IdentityCore.Models.Response;

namespace IdentityCore.Managers;

public class AuthenticationManager : IAuthenticationManager
{
    private readonly IRefreshTokenDbRepository _refTokenDbRepo;
    private readonly IRefreshTokenManager _refTokenManager;

    public AuthenticationManager(IRefreshTokenDbRepository refTokenDbRepo, IRefreshTokenManager refTokenManager)
    {
        _refTokenDbRepo = refTokenDbRepo;
        _refTokenManager = refTokenManager;
    }

    public async Task<OperationResult<LoginResponse>> CreateLoginTokensAsync(User user)
    {
        if (user is null)
            return new OperationResult<LoginResponse>("Invalid input data");

        var refreshToken = _refTokenManager.CreateRefreshToken(user);
        if (refreshToken is null)
            return new OperationResult<LoginResponse>("Error creating session");

        if (!await _refTokenManager.AddTokenAsync(user, refreshToken))
            return new OperationResult<LoginResponse>("Error creating session");

        var loginResponse = new LoginResponse
        {
            UserId = user.Id,
            Bearer = CreateJwt(user),
            RefreshToken = refreshToken.RefToken
        };

        return new OperationResult<LoginResponse>(loginResponse);
    }

    public async Task<OperationResult<LoginResponse>> RefreshLoginTokensAsync(RefreshToken token)
    {
        if (token is null)
            return new OperationResult<LoginResponse>("Invalid operation");

        var updatedToken = await _refTokenManager.UpdateTokenDbAsync(token);
        if (string.IsNullOrWhiteSpace(updatedToken))
            return new OperationResult<LoginResponse>("Invalid operation");

        var loginResponse = new LoginResponse
        {
            UserId = token.UserId,
            Bearer = CreateJwt(token.User),
            RefreshToken = updatedToken
        };

        return new OperationResult<LoginResponse>(loginResponse);
    }

    public async Task<string> LogoutAsync(Guid userId, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return "Invalid refresh token";

        var token = await _refTokenDbRepo.GetTokenByUserIdAsync(userId, refreshToken);
        if (token is null)
            return "The user was not found or was deleted";

        return await _refTokenDbRepo.DeleteAsync(token)
            ? string.Empty
            : "Error during deletion";
    }

    private static string CreateJwt(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var jwt = new JwtSecurityToken(
            issuer: JwtConfig.Values.Issuer,
            audience: JwtConfig.Values.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(JwtConfig.Values.Expires),
            signingCredentials: JwtConfig.Values.SigningCredentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}