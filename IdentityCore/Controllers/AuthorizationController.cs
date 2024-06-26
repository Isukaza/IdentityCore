using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Helpers;
using IdentityCore.DAL.Repository;
using IdentityCore.Managers;
using IdentityCore.Models.Request;
using IdentityCore.Models.Response;

namespace IdentityCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorizationController : ControllerBase
{
    #region C-tor and fields

    private readonly UserManager _userManager;
    private readonly RefreshTokenRepository _refreshTokenRepo;

    public AuthorizationController(UserManager userManager, RefreshTokenRepository refreshTokenRepository)
    {
        _userManager = userManager;
        _refreshTokenRepo = refreshTokenRepository;
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
    /// <returns>JWT token and Refresh token</returns>
    /// <response code="200">Successful login.</response>
    /// <response code="400">Email or password is invalid.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest loginRequest)
    {
        var result = await _userManager.ValidateUser(loginRequest);

        if (!result.Success)
            return await StatusCodes.Status400BadRequest.ResultState(result.ErrorMessage);

        var loginResponse = await _userManager.CreateLoginTokens(result.Data);

        return loginResponse.Success
            ? await StatusCodes.Status200OK.ResultState("Successful login", loginResponse.Data)
            : await StatusCodes.Status500InternalServerError.ResultState(loginResponse.ErrorMessage);
    }

    /// <summary>
    /// Logout user
    /// </summary>
    /// <param name="logoutRequest">JSON with logout data fields.</param>
    /// <returns>Exit status.</returns>
    /// <response code="200">Successful logout.</response>
    /// <response code="400">Error in tokens or user deleted.</response>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest logoutRequest)
    {
        var username = HttpContext.User.Claims
            .FirstOrDefault(claim => claim.Type == ClaimTypes.Name)?.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username))
            return await StatusCodes.Status400BadRequest.ResultState("Invalid jwt");

        var result = await _userManager.Logout(username, logoutRequest.RefreshToken);
        return result.Success
            ? await StatusCodes.Status200OK.ResultState()
            : await StatusCodes.Status400BadRequest.ResultState(result.ErrorMessage);
    }
}