using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

[Authorize]
[ApiController]
[Route("/[controller]")]
public class UserController : Controller
{
    #region C-tor and fields

    private readonly IUserManager _userManager;
    private readonly IMailManager _mailManager;
    private readonly ICfmTokenManager _ctManager;

    public UserController(IUserManager userManager, IMailManager mailManager, ICfmTokenManager ctManager)
    {
        _userManager = userManager;
        _mailManager = mailManager;
        _ctManager = ctManager;
    }

    #endregion

    #region CRUD

    /// <summary>
    /// Retrieves user information by user ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>Returns the user information if found.</returns>
    /// <response code="200">User information retrieved successfully.</response>
    /// <response code="401">Unauthorized. The user is not authenticated.</response>
    /// <response code="403">Forbidden. User does not have permission to access the requested information.</response>
    /// <response code="404">User with the specified ID not found.</response>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        var error = _userManager.ValidateUserIdentity(
            HttpContext.User.Claims.ToList(),
            userId,
            UserRole.Admin,
            (userRole, compareRole) => userRole >= compareRole);
        if (!string.IsNullOrEmpty(error))
            return await StatusCodes.Status403Forbidden.ResultState(error);

        var user = await _userManager.GetUserByIdAsync(userId);
        return user != null
            ? await StatusCodes.Status200OK.ResultState("User info", user.ToUserResponse())
            : await StatusCodes.Status404NotFound.ResultState($"User by id:{userId} not found");
    }

    /// <summary>
    /// Registers a new user in the system.
    /// </summary>
    /// <param name="userCreateRequest">An object containing the user's registration details.</param>
    /// <returns>Returns the status of the operation, including the user ID and token type.</returns>
    /// <response code="201">User registration successful, awaiting email confirmation.</response>
    /// <response code="400">Invalid registration data or email could not be sent.</response>
    /// <response code="500">An error occurred during user registration.</response>
    [AllowAnonymous]
    [HttpPost("registration")]
    [ProducesResponseType(typeof(ReSendCfmTokenResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegistrationUser([FromBody] UserCreateRequest userCreateRequest)
    {
        var errorMessage = await _userManager.ValidateRegistrationAsync(userCreateRequest);
        if (!string.IsNullOrEmpty(errorMessage))
            return await StatusCodes.Status400BadRequest.ResultState(errorMessage);

        var user = await _userManager.CreateUserForRegistrationAsync(userCreateRequest, Provider.Local);
        if (user is null)
            return await StatusCodes.Status500InternalServerError.ResultState("Error creating user");

        var cfmToken = _ctManager.CreateToken(user.Id, TokenType.RegistrationConfirmation);
        if (cfmToken is null)
        {
            _ = await _userManager.DeleteUserDataByTokenTypeAsync(
                user.Id,
                user.Username,
                user.Email,
                TokenType.RegistrationConfirmation);
            return await StatusCodes.Status500InternalServerError.ResultState("Error creating user");
        }

        var cfmLink = MailConfig.GetConfirmationLink(cfmToken.Value, cfmToken.TokenType);
        var sendMailError = await _mailManager.SendEmailAsync(user.Email, cfmToken.TokenType, cfmLink, user);

        var cfmTokenResponse = new ReSendCfmTokenResponse
        {
            UserId = cfmToken.UserId,
            TokenType = cfmToken.TokenType
        };
        return string.IsNullOrEmpty(sendMailError)
            ? await StatusCodes.Status201Created.ResultState("Awaiting confirmation by mail", cfmTokenResponse)
            : await StatusCodes.Status400BadRequest.ResultState("Not send mail");
    }

    /// <summary>
    /// Updates the details of an existing user.
    /// </summary>
    /// <param name="updateRequest">The new details of the user.</param>
    /// <returns>Returns the status of the update.</returns>
    /// <response code="200">User updated successfully.</response>
    /// <response code="400">The update data provided is invalid.</response>
    /// <response code="401">Unauthorized. The user is not authenticated.</response>
    /// <response code="403">Forbidden. The user does not have permission to perform this action.</response>
    /// <response code="404">User with the specified ID not found.</response>
    /// <response code="500">An error occurred during the user update process.</response>
    [HttpPut("update")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)}, {nameof(UserRole.Admin)}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> UserUpdate([FromBody] SuUserUpdateRequest updateRequest)
    {
        var userIdFromClaim = HttpContext.User.Claims.GetUserId();
        var role = HttpContext.User.Claims.GetUserRole();
        if (role is null || userIdFromClaim is null)
            return await StatusCodes.Status403Forbidden
                .ResultState("Authorization failed due to an invalid or missing role in the provided token");

        var tokenType = _ctManager.DetermineTokenType(updateRequest);
        if (tokenType is TokenType.Unknown)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid input data");

        if (role == UserRole.Admin && tokenType != TokenType.RoleChange)
            return await StatusCodes.Status403Forbidden.ResultState("Admin can only change roles");

        if (role == UserRole.Admin && (updateRequest.Role != UserRole.Manager || updateRequest.Role != UserRole.User))
            return await StatusCodes.Status403Forbidden.ResultState("Admin can't assign higher than manager");

        if (!await _userManager.UserExistsByIdAsync(updateRequest.Id))
            return await StatusCodes.Status404NotFound.ResultState("User not found");

        var result = await _userManager.ValidateUserUpdateAsync(updateRequest);
        if (!result.Success)
            return await StatusCodes.Status400BadRequest.ResultState(result.ErrorMessage);

        if (result.Data.Role == UserRole.SuperAdmin && role == UserRole.Admin)
            return await StatusCodes.Status403Forbidden.ResultState("You don't have access to SU update");

        if (result.Data.Role == UserRole.SuperAdmin
            && role == UserRole.SuperAdmin
            && userIdFromClaim != updateRequest.Id)
            return await StatusCodes.Status403Forbidden.ResultState("Cannot update other SU");

        return await _userManager.UpdateUser(result.Data, updateRequest)
            ? await StatusCodes.Status200OK.ResultState("Operation was successfully completed")
            : await StatusCodes.Status500InternalServerError.ResultState("Error updating user");
    }

    /// <summary>
    /// Initiates a request to update the data of an existing user, and sends a request for email confirmation.
    /// </summary>
    /// <param name="updateRequest">The new details of the user.</param>
    /// <returns>Returns the status of the update.</returns>
    /// <response code="200">User updated successfully, awaiting email confirmation.</response>
    /// <response code="400">The update data provided is invalid or does not meet validation criteria.</response>
    /// <response code="401">Unauthorized. The user is not authenticated.</response>
    /// <response code="403">Forbidden. The user does not have permission to access or update the requested data.</response>
    /// <response code="429">A user update is already in progress. Please complete the current update process.</response>
    /// <response code="500">An error occurred during the user update process, or the email could not be sent.</response>
    [HttpPut("request-update")]
    [ProducesResponseType(typeof(ReSendCfmTokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestUserUpdate([FromBody] UserUpdateRequest updateRequest)
    {
        var error = _userManager.ValidateUserIdentity(HttpContext.User.Claims.ToList(), updateRequest.Id);
        if (!string.IsNullOrEmpty(error))
            return await StatusCodes.Status403Forbidden.ResultState(error);

        if (await _userManager.IsUserUpdateInProgressAsync(updateRequest.Id))
            return await StatusCodes.Status429TooManyRequests.ResultState("Complete the current update process");

        var result = await _userManager.ValidateUserUpdateAsync(updateRequest);
        if (!result.Success)
            return await StatusCodes.Status400BadRequest.ResultState(result.ErrorMessage);

        var tokenType = _ctManager.DetermineTokenType(updateRequest);
        if (tokenType is TokenType.Unknown || tokenType is TokenType.RoleChange)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid input data");

        var redisUserUpdateData = updateRequest.ToRedisUserUpdate();
        var ttl = TokenConfig.GetTtlForTokenType(tokenType);
        var redisUserUpdate = _userManager.AddUserUpdateDataByTokenType(redisUserUpdateData, tokenType, ttl);
        if (redisUserUpdate is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid input data");

        var cfmToken = _ctManager.CreateToken(result.Data.Id, tokenType);
        var cfmLink = MailConfig.GetConfirmationLink(cfmToken.Value, cfmToken.TokenType);
        var sendMailError = await _mailManager
            .SendEmailAsync(result.Data.Email, tokenType, cfmLink, result.Data, redisUserUpdate);

        var cfmTokenResponse = new ReSendCfmTokenResponse
        {
            UserId = cfmToken.UserId,
            TokenType = cfmToken.TokenType
        };
        return string.IsNullOrEmpty(sendMailError)
            ? await StatusCodes.Status200OK.ResultState("Awaiting confirmation by mail", cfmTokenResponse)
            : await StatusCodes.Status500InternalServerError.ResultState("Failed to send mail");
    }

    /// <summary>
    /// Initiates a password reset request for the specified email address.
    /// </summary>
    /// <param name="email">The email address associated with the user account for which the password reset is requested.</param>
    /// <returns>Returns the status of the password reset request.</returns>
    /// <response code="200">Password reset request was processed successfully.</response>
    /// <response code="400">The email address provided is invalid or does not meet validation criteria.</response>
    /// <response code="500">An error occurred during the password reset process or the email could not be sent.</response>
    [HttpPost("request-password-reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestPasswordReset([Required] [EmailAddress] string email)
    {
        var user = await _userManager.GetUserByEmailAsync(email);
        if (user == null)
            return await StatusCodes.Status200OK.ResultState();

        const TokenType tokenType = TokenType.PasswordReset;
        var existingToken = await _ctManager.GetTokenByUserIdAsync(user.Id, tokenType);
        if (existingToken != null)
        {
            var timeForNextAttempt = _ctManager.GetNextAttemptTime(existingToken);
            if (!string.IsNullOrEmpty(timeForNextAttempt))
                return await StatusCodes.Status200OK.ResultState();
        }

        var cfmToken = existingToken is null
            ? _ctManager.CreateToken(user.Id, tokenType)
            : await _ctManager.UpdateTokenAsync(existingToken);

        var cfmLink = MailConfig.GetConfirmationLink(cfmToken.Value, cfmToken.TokenType);
        _ = await _mailManager.SendEmailAsync(email, tokenType, cfmLink, user);

        return await StatusCodes.Status200OK.ResultState();
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

        var userUpd = await _userManager.GetUserByIdAsync<RedisUserUpdate>(RedisPrefixes.User.Update, tokenDb.UserId);
        if (userUpd is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var cfmLink = MailConfig.GetConfirmationLink(tokenDb.Value, tokenDb.TokenType);
        var sendMailError = await _mailManager
            .SendEmailAsync(userUpd.Email, tokenDb.TokenType, cfmLink, user, userUpd);

        var cfmTokenResponse = new ReSendCfmTokenResponse
        {
            UserId = tokenDb.UserId,
            TokenType = TokenType.EmailChangeNew
        };
        return string.IsNullOrEmpty(sendMailError)
            ? await StatusCodes.Status200OK
                .ResultState("Confirmation email sent to the new email address", cfmTokenResponse)
            : await StatusCodes.Status500InternalServerError.ResultState(sendMailError);
    }

    /// <summary>
    /// Deletes an existing user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to be deleted.</param>
    /// <returns>Returns the status of the deletion.</returns>
    /// <response code="200">User deleted successfully.</response>
    /// <response code="401">Unauthorized. The user is not authenticated.</response>
    /// <response code="403">Forbidden. User does not have permission to delete the requested user.</response>
    /// <response code="404">The user with the specified ID was not found.</response>
    /// <response code="500">An error occurred during the user deletion.</response>
    [HttpDelete("delete")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteUser([Required] Guid userId)
    {
        var error = _userManager.ValidateUserIdentity(
            HttpContext.User.Claims.ToList(),
            userId,
            UserRole.SuperAdmin,
            (userRole, compareRole) => userRole == compareRole);
        if (!string.IsNullOrEmpty(error))
            return await StatusCodes.Status403Forbidden.ResultState(error);

        var userToDeleted = await _userManager.GetUserByIdAsync(userId);
        if (userToDeleted is null)
            return await StatusCodes.Status404NotFound.ResultState("User not found");

        if (userToDeleted.Role == UserRole.SuperAdmin)
            return await StatusCodes.Status403Forbidden.ResultState("SU cannot be deleted");

        return await _userManager.DeleteUserAsync(userToDeleted)
            ? await StatusCodes.Status200OK.ResultState("User deleted")
            : await StatusCodes.Status500InternalServerError.ResultState("Error when deleting user");
    }

    #endregion

    #region Confirmation action

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
                var ttl = TokenConfig.GetTtlForTokenType(TokenType.EmailChangeNew);
                _ = await _userManager.UpdateTtlUserUpdateByTokenTypeAsync(userUpd, token.TokenType, ttl);
            }
            else
            {
                var userToDelete = token.TokenType == TokenType.RegistrationConfirmation
                    ? new { user.Id, user.Username, user.Email }
                    : new { userUpd!.Id, userUpd.Username, userUpd.Email };

                _ = await _userManager.DeleteUserDataByTokenTypeAsync(
                    userToDelete.Id,
                    userToDelete.Username,
                    userToDelete.Email,
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

        var ttl = TokenConfig.GetTtlForTokenType(token.TokenType);
        var updateData = token.TokenType == TokenType.RegistrationConfirmation
            ? user.ToRedisUserUpdate()
            : userUpd;

        _ = await _userManager.UpdateTtlUserUpdateByTokenTypeAsync(updateData, token.TokenType, ttl);

        var cfmLink = MailConfig.GetConfirmationLink(updatedToken.Value, updatedToken.TokenType);
        var email = token.TokenType == TokenType.EmailChangeNew && userUpd is not null
            ? userUpd.Email
            : user.Email;

        var sendMailError = await _mailManager.SendEmailAsync(email, token.TokenType, cfmLink, user, userUpd);
        return string.IsNullOrEmpty(sendMailError)
            ? await StatusCodes.Status200OK.ResultState("Operation was successfully completed")
            : await StatusCodes.Status400BadRequest.ResultState("Not send mail");
    }

    #endregion

    #region CommonMethods

    /// <summary>
    /// Generates a salt string of the specified size.
    /// </summary>
    /// <param name="size">The size of the salt in characters (must be between 16 and 64).</param>
    /// <returns>Returns the generated salt string.</returns>
    /// <response code="200">Salt generated successfully.</response>
    /// <response code="400">Bad request. The salt size is not within the valid range (16-64).</response>
    /// <response code="401">Unauthorized. The user is not authenticated.</response>
    /// <response code="403">Forbidden. The user does not have permission to generate the salt.</response>
    [HttpGet("salt/{size:int}")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)}, {nameof(UserRole.Admin)}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenSalt(int size)
    {
        if (size < 16 || size > 64)
            return await StatusCodes.Status400BadRequest
                .ResultState("The salt size must be greater than 16 and less than 64 characters");

        var salt = UserHelper.GenerateSalt(size);
        return await StatusCodes.Status200OK.ResultState("Salt", salt);
    }

    /// <summary>
    /// Generates a random password of the specified size.
    /// </summary>
    /// <param name="size">The size of the password in characters (must be between 12 and 64).</param>
    /// <returns>Returns the generated password string.</returns>
    /// <response code="200">Password generated successfully.</response>
    /// <response code="400">Bad request. The password size is not within the valid range (12-64).</response>
    /// <response code="401">Unauthorized. The user is not authenticated.</response>
    /// <response code="403">Forbidden. The user does not have permission to generate a password.</response>
    [HttpGet("password/{size:int}")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)}, {nameof(UserRole.Admin)}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenPassword(int size)
    {
        if (size < 12 || size > 64)
            return await StatusCodes.Status400BadRequest
                .ResultState("The password size must be greater than 12 and less than 64 characters");

        var pass = UserHelper.GeneratePassword(size);
        return await StatusCodes.Status200OK.ResultState("Password", pass);
    }

    /// <summary>
    /// Generates a hash for the provided password using the provided salt.
    /// </summary>
    /// <param name="password">The password string to be hashed (must be between 12 and 64 characters).</param>
    /// <param name="salt">The salt string to be used in hashing (must be between 16 and 64 characters).</param>
    /// <returns>Returns the hashed password string.</returns>
    /// <response code="200">Password hash generated successfully.</response>
    /// <response code="400">Bad request. Either the password or salt size is not within the valid range.</response>
    /// <response code="401">Unauthorized. The user is not authenticated.</response>
    /// <response code="403">Forbidden. The user does not have permission to generate a password hash.</response>
    [HttpGet("password-hash")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)}, {nameof(UserRole.Admin)}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPasswordHash(string password, string salt)
    {
        if (password.Length < 12 || password.Length > 64)
            return await StatusCodes.Status400BadRequest
                .ResultState("The password size must be greater than 12 and less than 64 characters");

        if (salt.Length < 16 || salt.Length > 64)
            return await StatusCodes.Status400BadRequest
                .ResultState("The salt size must be greater than 16 and less than 64 characters");

        var passwordHash = UserHelper.GetPasswordHash(password, salt);
        return await StatusCodes.Status200OK.ResultState("Password hash", passwordHash);
    }

    #endregion
}