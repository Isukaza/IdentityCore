using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.Repository;
using IdentityCore.Managers;
using IdentityCore.Models.Request;
using IdentityCore.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace IdentityCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorizationController : ControllerBase
{
    #region C-tor and fields

    private readonly UserRepository _userRepo;
    private readonly UserManager _userManager;

    public AuthorizationController(UserRepository userRepository, UserManager userManager)
    {
        _userRepo = userRepository;
        _userManager = userManager;
    }

    #endregion
    
    [HttpPost("register")]
    public async Task<IActionResult> Register()
    {
        return await StatusCodes.Status200OK.ResultState();
    }

    /// <summary>
    /// Login user
    /// </summary>
    /// <param name="loginRequest">JSON with authorization data fields.</param>
    /// <returns>JWT token</returns>
    /// <response code="200">Successful login.</response>
    /// <response code="400">Email or password is invalid.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest loginRequest)
    {
        var result = await _userManager.ValidateUser(loginRequest);

        if (!result.Success)
            return await StatusCodes.Status400BadRequest.ResultState(result.ErrorMessage);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, result.Data.Username),
            new(ClaimTypes.Email, result.Data.Email),
            new(ClaimTypes.Role, "Admin")
        };

        var jwt = new JwtSecurityToken(
            issuer: Jwt.Configs.Issuer,
            audience: Jwt.Configs.Audience,
            claims: claims,
            expires: Jwt.Configs.Expires,
            signingCredentials: new SigningCredentials(Jwt.Configs.Key, SecurityAlgorithms.HmacSha256));
        
        return await StatusCodes.Status200OK.ResultState("Successful login", new JwtSecurityTokenHandler().WriteToken(jwt));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        return await StatusCodes.Status200OK.ResultState();
    }
}