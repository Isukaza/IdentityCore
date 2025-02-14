using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using RabbitMQ.Messaging.Models;

using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL.Models.cache;
using IdentityCore.DAL.PostgreSQL.Models.cache.cachePrefix;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.DAL.PostgreSQL.Roles;
using IdentityCore.Managers.Interfaces;
using IdentityCore.Models;
using IdentityCore.Models.Request;
using IdentityCore.Models.Response;

namespace IdentityCore.Controllers;

[ApiController]
[Route("/[controller]")]
public class ConfirmationController : Controller
{
    #region C-tor and fields

    private readonly IUserManager _userManager;
    private readonly IMessageSenderManager _messageSenderManager;
    private readonly ICfmTokenManager _ctManager;

    public ConfirmationController(
        IUserManager userManager,
        IMessageSenderManager messageSenderManager,
        ICfmTokenManager ctManager)
    {
        _userManager = userManager;
        _messageSenderManager = messageSenderManager;
        _ctManager = ctManager;
    }

    #endregion

    #region Confirmation action

    /// <summary>
    /// Sends a confirmation email for a new email address change.
    /// </summary>
    /// <remarks>
    /// This method should be called only after the old email address has been confirmed during the email change process.
    /// </remarks>
    /// <returns>Returns the status of the email confirmation process.</returns>
    /// <response code="200">Confirmation email sent successfully.</response>
    /// <response code="400">The provided token or user data is invalid.</response>
    /// <response code="401">Unauthorized. The user is not authenticated.</response>
    /// <response code="403">Forbidden. The user does not have permission to access the requested information.</response>
    /// <response code="500">An error occurred during the email confirmation process.</response>
    [HttpGet("send-new-email-confirmation")]
    [ProducesResponseType(typeof(ReSendCfmTokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendNewEmailConfirmation()
    {
        var userId = HttpContext.User.Claims.GetUserId();
        if (!userId.HasValue)
            return await StatusCodes.Status403Forbidden
                .ResultState("Authorization failed due to an invalid or missing role in the provided token");

        var tokenDb = await _ctManager.GetTokenByUserIdAsync(userId.Value, TokenType.EmailChangeNew);
        if (tokenDb is null || tokenDb.UserId != userId.Value)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var user = await _userManager.GetUserByIdAsync(userId.Value);
        if (user is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var userUpd = await _userManager
            .GetUserByIdAsync<RedisUserUpdate>(RedisPrefixes.User.Update, tokenDb.UserId);
        if (userUpd is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        Console.WriteLine(tokenDb.Value);

        var cfmLink = MailConfig.GetConfirmationLink(tokenDb.Value, tokenDb.TokenType);
        var sendMessageError = await _messageSenderManager
            .SendMessageAsync(userUpd.ToUserUpdateMessage(user, cfmLink));

        var cfmTokenResponse = new ReSendCfmTokenResponse
        {
            UserId = tokenDb.UserId,
            TokenType = TokenType.EmailChangeNew
        };

        return string.IsNullOrEmpty(sendMessageError)
            ? await StatusCodes.Status200OK
                .ResultState("Confirmation email sent to the new email address", cfmTokenResponse)
            : await StatusCodes.Status500InternalServerError.ResultState();
    }

    /// <summary>
    /// Confirms a registration token.
    /// </summary>
    /// <param name="tokenRequest">The token request details.</param>
    /// <returns>Returns the status of the token confirmation.</returns>
    /// <response code="200">Token confirmed successfully.</response>
    /// <response code="400">The provided token is invalid.</response>
    /// <response code="500">An error occurred during token confirmation.</response>
    [AllowAnonymous]
    [HttpGet("cfm-reg-token")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmationRegToken([FromQuery] CfmTokenRequest tokenRequest)
    {
        return await HandleTokenConfirmation(tokenRequest, true);
    }

    /// <summary>
    /// Confirms a token.
    /// </summary>
    /// <param name="tokenRequest">The token request details.</param>
    /// <returns>Returns the status of the token confirmation.</returns>
    /// <response code="200">Token confirmed successfully.</response>
    /// <response code="400">The provided token is invalid.</response>
    /// <response code="401">Unauthorized. The user is not authenticated.</response>
    /// <response code="500">An error occurred during token confirmation.</response>
    [HttpGet("cfm-token")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmationToken([FromQuery] CfmTokenRequest tokenRequest)
    {
        return await HandleTokenConfirmation(tokenRequest, false);
    }

    /// <summary>
    /// Resends a registration confirmation token.
    /// </summary>
    /// <param name="tokenRequest">The token request details.</param>
    /// <returns>Returns the status of the token resend process.</returns>
    /// <response code="200">Token resent successfully.</response>
    /// <response code="400">The provided token or user data is invalid.</response>
    /// <response code="500">An error occurred during the token resend process.</response>
    [AllowAnonymous]
    [HttpPost("resend-cfm-reg-token")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendCfmRegToken(ReSendCfmTokenRequest tokenRequest)
    {
        return await HandleTokenResend(tokenRequest, true);
    }

    /// <summary>
    /// Resends a confirmation token.
    /// </summary>
    /// <param name="tokenRequest">The token request details.</param>
    /// <returns>Returns the status of the token resend process.</returns>
    /// <response code="200">Token resent successfully.</response>
    /// <response code="400">The provided token or user data is invalid.</response>
    /// <response code="401">Unauthorized. The user is not authenticated.</response>
    /// <response code="403">Forbidden. The user does not have permission to access this resource.</response>
    /// <response code="500">An error occurred during the token resend process.</response>
    [HttpPost("resend-cfm-token")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendCfmToken(ReSendCfmTokenRequest tokenRequest)
    {
        var error = _userManager.ValidateUserIdentity(HttpContext.User.Claims.ToList(), tokenRequest.UserId);
        if (!string.IsNullOrEmpty(error))
            return await StatusCodes.Status403Forbidden.ResultState(error);

        return await HandleTokenResend(tokenRequest, false);
    }

    /// <summary>
    /// Confirms a password reset request using the provided token and new password.
    /// </summary>
    /// <param name="passwordResetCfm">The password reset confirmation request containing the token and new password.</param>
    /// <returns>Returns the status of the password reset confirmation process.</returns>
    /// <response code="200">Password reset confirmed successfully. The user's password has been updated.</response>
    /// <response code="400">The provided token is invalid, or the input data does not meet validation criteria.</response>
    /// <response code="500">An error occurred during the password reset process.</response>
    [AllowAnonymous]
    [HttpPost("password-reset/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmPasswordReset([FromBody] PasswordResetCfm passwordResetCfm)
    {
        var token = await _ctManager.GetTokenAsync(passwordResetCfm.Token, TokenType.PasswordReset);
        if (token is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid input data");

        var user = await _userManager.GetUserByIdAsync(token.UserId);
        if (user is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid input data");

        var userUpd = _userManager.GeneratePasswordUpdateEntityAsync(passwordResetCfm.Password);
        if (userUpd is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid input data");

        var executionError = await _userManager.ExecuteUserUpdateFromTokenAsync(user, userUpd, token);
        if (string.IsNullOrEmpty(executionError))
            _ = await _ctManager.DeleteTokenAsync(token);

        return string.IsNullOrEmpty(executionError)
            ? await StatusCodes.Status200OK.ResultState("Operation was successfully completed")
            : await StatusCodes.Status500InternalServerError.ResultState(executionError);
    }

    private async Task<IActionResult> HandleTokenConfirmation(CfmTokenRequest tokenRequest, bool isRegistrationProcess)
    {
        if (!_ctManager.ValidateTokenTypeForRequest(tokenRequest.TokenType, isRegistrationProcess))
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var token = await _ctManager.GetTokenAsync(tokenRequest.Token, tokenRequest.TokenType);
        if (token is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var user = await _userManager.GetUserByTokenTypeAsync(token.UserId, token.TokenType);
        if (user is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var userUpd = await _userManager.GetUserByIdAsync<RedisUserUpdate>(RedisPrefixes.User.Update, token.UserId);
        if (userUpd is null && token.TokenType != TokenType.RegistrationConfirmation)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var executionError = await _userManager.ExecuteUserUpdateFromTokenAsync(user, userUpd, token);
        if (string.IsNullOrEmpty(executionError))
        {
            _ = await _ctManager.DeleteTokenAsync(token);
            if (tokenRequest.TokenType == TokenType.EmailChangeOld)
            {
                _ = await _userManager.UpdateTtlUserUpdateByTokenTypeAsync(
                    userUpd!.Id,
                    null,
                    userUpd.NewValue,
                    token.TokenType);
            }
            else
            {
                var userDataToDelete = token.TokenType switch
                {
                    TokenType.RegistrationConfirmation => new { user.Id, user.Username, user.Email },
                    TokenType.UsernameChange => new { userUpd!.Id, Username = userUpd.NewValue, Email = string.Empty },
                    TokenType.EmailChangeOld or TokenType.EmailChangeNew =>
                        new { userUpd!.Id, Username = string.Empty, Email = userUpd.NewValue },
                    _ => new { userUpd!.Id, Username = string.Empty, Email = string.Empty }
                };

                _ = await _userManager.DeleteUserDataByTokenTypeAsync(
                    userDataToDelete.Id,
                    userDataToDelete.Username,
                    userDataToDelete.Email,
                    token.TokenType);
            }
        }

        return string.IsNullOrEmpty(executionError)
            ? await StatusCodes.Status200OK.ResultState("Operation was successfully completed")
            : await StatusCodes.Status500InternalServerError.ResultState(executionError);
    }

    private async Task<IActionResult> HandleTokenResend(ReSendCfmTokenRequest tokenRequest, bool isRegistration)
    {
        if (!_ctManager.ValidateTokenTypeForRequest(tokenRequest.TokenType, isRegistration))
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var token = await _ctManager.GetTokenByUserIdAsync(tokenRequest.UserId, tokenRequest.TokenType);
        if (token is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var timeForNextAttempt = _ctManager.GetNextAttemptTime(token);
        if (!string.IsNullOrEmpty(timeForNextAttempt))
            return await StatusCodes.Status429TooManyRequests.ResultState(timeForNextAttempt);

        var user = await _userManager.GetUserByTokenTypeAsync(token.UserId, token.TokenType);
        if (user is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var userUpd = await _userManager.GetUserByIdAsync<RedisUserUpdate>(RedisPrefixes.User.Update, token.UserId);
        if (token.TokenType != TokenType.RegistrationConfirmation && userUpd == null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var updatedToken = await _ctManager.UpdateTokenAsync(token);
        if (updatedToken is null)
            return await StatusCodes.Status500InternalServerError.ResultState("Failed to send verification token");

        var updateData = token.TokenType switch
        {
            TokenType.RegistrationConfirmation => new { user.Id, user.Username, user.Email },
            TokenType.UsernameChange => new { userUpd!.Id, Username = userUpd.NewValue, Email = string.Empty },
            TokenType.EmailChangeOld or TokenType.EmailChangeNew =>
                new { userUpd!.Id, Username = string.Empty, Email = userUpd.NewValue },
            _ => new { userUpd!.Id, Username = string.Empty, Email = string.Empty }
        };

        _ = await _userManager.UpdateTtlUserUpdateByTokenTypeAsync(
            updateData.Id,
            updateData.Username,
            updateData.Email,
            token.TokenType);

        var cfmLink = MailConfig.GetConfirmationLink(updatedToken.Value, updatedToken.TokenType);
        var email = token.TokenType == TokenType.EmailChangeNew && userUpd is not null
            ? userUpd.NewValue
            : user.Email;

        var oldValue = updatedToken.TokenType switch
        {
            TokenType.UsernameChange => user.Username,
            TokenType.EmailChangeOld or TokenType.EmailChangeNew => user.Email,
            _ => string.Empty
        };

        var sendMessageError = await _messageSenderManager
            .SendMessageAsync(new UserUpdateMessage
            {
                UserEmail = email,
                UserName = user.Username,
                NewValue = userUpd?.NewValue,
                OldValue = oldValue,
                ChangeType = updatedToken.TokenType,
                ConfirmationLink = cfmLink,
            });

        Console.WriteLine(updatedToken);

        return string.IsNullOrEmpty(sendMessageError)
            ? await StatusCodes.Status200OK.ResultState("Operation was successfully completed")
            : await StatusCodes.Status400BadRequest.ResultState("Not send mail");
    }

    #endregion
}