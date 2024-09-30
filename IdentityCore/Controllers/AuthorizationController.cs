using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Helpers;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Roles;
using IdentityCore.Managers.Interfaces;
using IdentityCore.Models.Request;
using IdentityCore.Models.Response;

namespace IdentityCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorizationController : Controller
{
    #region C-tor and fields

    private readonly IUserManager _userManager;
    private readonly IAuthenticationManager _authenticationManager;
    private readonly IRefreshTokenManager _refreshTokenManager;
    private readonly IGoogleManager _googleManager;

    public AuthorizationController(
        IUserManager userManager,
        IAuthenticationManager authenticationManager,
        IRefreshTokenManager refreshTokenManager,
        IGoogleManager googleManager)
    {
        _userManager = userManager;
        _authenticationManager = authenticationManager;
        _refreshTokenManager = refreshTokenManager;
        _googleManager = googleManager;
    }

    #endregion

    /// <summary>
    /// Log in a user and return JWT and Refresh token.
    /// </summary>
    /// <param name="loginRequest">JSON object containing user email and password.</param>
    /// <returns>Returns a LoginResponse containing JWT and Refresh token.</returns>
    /// <response code="200">Successful login with JWT and Refresh token returned.</response>
    /// <response code="400">Invalid email or password. Returns an error message detailing the issue.</response>
    /// <response code="500">Internal server error. Returns an error message if there’s an issue generating tokens.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest loginRequest)
    {
        var result = await _userManager.ValidateLoginAsync(loginRequest);
        if (!result.Success)
            return await StatusCodes.Status400BadRequest.ResultState(result.ErrorMessage);

        var loginResponse = await _authenticationManager.CreateLoginTokensAsync(result.Data);
        return loginResponse.Success
            ? await StatusCodes.Status200OK.ResultState("Successful login", loginResponse.Data)
            : await StatusCodes.Status500InternalServerError.ResultState(loginResponse.ErrorMessage);
    }

    /// <summary>
    /// Initiates the Google OAuth2 login process by redirecting to Google's authorization endpoint.
    /// </summary>
    /// <returns>Redirects to Google OAuth2 authorization URL.</returns>
    /// <response code="302">Redirect to Google login page.</response>
    /// <response code="500">Internal server error if unable to generate Google login URL.</response>
    [AllowAnonymous]
    [HttpGet("google-login")]
    public IActionResult GoogleLogin()
    {
        var authorizationUrl = _googleManager.GenerateGoogleLoginUrl();
        return Redirect(authorizationUrl);
    }

    /// <summary>
    /// Handles the Google OAuth2 callback and exchanges the authorization code for tokens.
    /// </summary>
    /// <param name="code">The authorization code received from Google.</param>
    /// <returns>JWT token and Refresh token.</returns>
    /// <response code="200">Successful login and token generation.</response>
    /// <response code="400">Invalid Google token or bad request.</response>
    /// <response code="500">Error creating or updating user or generating tokens.</response>
    [AllowAnonymous]
    [HttpGet("google-callback")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GoogleCallback(string code)
    {
        var tokenResponse = await _googleManager.ExchangeCodeForTokenAsync(code);
        var payload = await _googleManager.VerifyGoogleTokenAsync(tokenResponse.IdToken);
        if (payload == null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid Google token.");
        
        var userSso = await _userManager.UserExistsByEmailAsync(payload.Email)
            ? await _userManager.GetUserSsoAsync(payload.Email)
            : await _userManager.CreateUserSsoAsync(payload.Email, payload.Name, Provider.Google);

        if (!userSso.Success)
            return await StatusCodes.Status500InternalServerError.ResultState(userSso.ErrorMessage);

        var loginResponse = await _authenticationManager.CreateLoginTokensAsync(userSso.Data);
        return loginResponse.Success
            ? await StatusCodes.Status200OK.ResultState("Successful login", loginResponse.Data)
            : await StatusCodes.Status500InternalServerError.ResultState(loginResponse.ErrorMessage);
    }

    /// <summary>
    /// Refreshes JWT access and Refresh tokens using the provided refresh token.
    /// </summary>
    /// <param name="refreshTokenRequest">JSON object with data fields to update the access token.</param>
    /// <returns>JWT Access token and Refresh token.</returns>
    /// <response code="200">Successful refresh.</response>
    /// <response code="400">Invalid input data or token validation failed.</response>
    /// <response code="403">Authorization failed due to an invalid or missing role in the provided token.</response>
    [HttpPost("refresh")]
    [Authorize]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest refreshTokenRequest)
    {
        var userId = HttpContext.User.Claims.GetUserId();
        if (!userId.HasValue)
            return await StatusCodes.Status403Forbidden
                .ResultState("Authorization failed due to an invalid or missing role in the provided token");
        
        var result = await _refreshTokenManager
            .ValidationRefreshTokenAsync(userId.Value, refreshTokenRequest.RefreshToken);

        if (!result.Success)
            return await StatusCodes.Status400BadRequest.ResultState(result.ErrorMessage);

        var loginResponse = await _authenticationManager.RefreshLoginTokensAsync(result.Data);
        return loginResponse.Success
            ? await StatusCodes.Status200OK.ResultState("Successful login refresh", loginResponse.Data)
            : await StatusCodes.Status400BadRequest.ResultState(loginResponse.ErrorMessage);
    }

    /// <summary>
    /// Logs out the user and invalidates the current session tokens.
    /// </summary>
    /// <param name="logoutRequest">JSON object containing logout data fields.</param>
    /// <returns>Status of the logout operation.</returns>
    /// <response code="200">Successful logout.</response>
    /// <response code="400">Error in tokens or user deleted.</response>
    /// <response code="403">Authorization failed due to an invalid or missing role in the provided token.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest logoutRequest)
    {
        var userId = HttpContext.User.Claims.GetUserId();
        if (!userId.HasValue)
            return await StatusCodes.Status403Forbidden
                .ResultState("Authorization failed due to an invalid or missing role in the provided token");
        
        var errorMessage = await _authenticationManager.LogoutAsync(userId.Value, logoutRequest.RefreshToken);
        return string.IsNullOrEmpty(errorMessage)
            ? await StatusCodes.Status200OK.ResultState()
            : await StatusCodes.Status400BadRequest.ResultState(errorMessage);
    }
}