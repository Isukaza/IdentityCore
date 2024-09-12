using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL.Models.db;
using IdentityCore.DAL.PostgreSQL.Repositories.Interfaces;
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
        var refreshToken = _refTokenManager.CreateRefreshToken(user);
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

    private static string CreateJwt(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, "Admin")
        };

        var jwt = new JwtSecurityToken(
            issuer: Jwt.Configs.Issuer,
            audience: Jwt.Configs.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(Jwt.Configs.Expires),
            signingCredentials: new SigningCredentials(Jwt.Configs.Key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
    
    public async Task<OperationResult<LoginResponse>> RefreshLoginTokensAsync(RefreshToken token)
    {
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
        var token = await _refTokenDbRepo.GetTokenByUserIdAsync(userId, refreshToken);
        if (token is null)
            return "The user was not found or was deleted";

        return await _refTokenDbRepo.DeleteAsync(token)
            ? string.Empty
            : "Error during deletion";
    }
}