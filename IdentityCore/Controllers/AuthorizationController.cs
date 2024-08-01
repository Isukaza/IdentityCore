using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Helpers;
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
    private readonly RefreshTokenManager _refreshTokenManager;

    public AuthorizationController(UserManager userManager, RefreshTokenManager refreshTokenManager)
    {
        _userManager = userManager;
        _refreshTokenManager = refreshTokenManager;
    }

    #endregion

    /// <summary>
    /// Login user
    /// </summary>
    /// <param name="loginRequest">JSON with authorization data fields.</param>
    /// <returns>JWT token and Refresh token.</returns>
    /// <response code="200">Successful login.</response>
    /// <response code="400">Email or password is invalid.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest loginRequest)
    {
        var result = await _userManager.ValidateLogin(loginRequest);
        if (!result.Success)
            return await StatusCodes.Status400BadRequest.ResultState(result.ErrorMessage);

        var loginResponse = await _userManager.CreateLoginTokens(result.Data);
        return loginResponse.Success
            ? await StatusCodes.Status200OK.ResultState("Successful login", loginResponse.Data)
            : await StatusCodes.Status500InternalServerError.ResultState(loginResponse.ErrorMessage);
    }

    /// <summary>
    /// Get new JWT access tokens and Refresh Tokens using the refresh token.
    /// </summary>
    /// <param name="refreshTokenRequest">JSON with data fields to update the access token.</param>
    /// <returns>JWT Access token and Refresh token.</returns>
    /// <response code="200">Successful refresh.</response>
    /// <response code="400">Invalid input data.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest refreshTokenRequest)
    {
        var result = await _refreshTokenManager
            .ValidationRefreshToken(refreshTokenRequest.UserId, refreshTokenRequest.RefreshToken);

        if (!result.Success)
            return await StatusCodes.Status400BadRequest.ResultState(result.ErrorMessage);

        var loginResponse = await _userManager.RefreshLoginTokens(result.Data);
        return loginResponse.Success
            ? await StatusCodes.Status200OK.ResultState("Successful login refresh", loginResponse.Data)
            : await StatusCodes.Status400BadRequest.ResultState(loginResponse.ErrorMessage);
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
        var errorMessage = await _userManager.Logout(logoutRequest.UserId, logoutRequest.RefreshToken);
        return string.IsNullOrEmpty(errorMessage)
            ? await StatusCodes.Status200OK.ResultState()
            : await StatusCodes.Status400BadRequest.ResultState(errorMessage);
    }
}