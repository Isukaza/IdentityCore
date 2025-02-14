using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using RabbitMQ.Messaging.Models;

using Helpers;
using IdentityCore.Configuration;
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
    private readonly ICfmTokenManager _ctManager;
    private readonly IMessageSenderManager _messageSenderManager;

    public UserController(
        IUserManager userManager,
        IMessageSenderManager messageSenderManager,
        ICfmTokenManager ctManager)
    {
        _userManager = userManager;
        _ctManager = ctManager;
        _messageSenderManager = messageSenderManager;
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
        var sendMessageError = await _messageSenderManager.SendMessageAsync(user.ToUserUpdateMessage(cfmLink));

        var cfmTokenResponse = new ReSendCfmTokenResponse
        {
            UserId = cfmToken.UserId,
            TokenType = cfmToken.TokenType
        };

        return string.IsNullOrEmpty(sendMessageError)
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

    #region ModificationRequests

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

        var redisUserUpdateData = _userManager.CreateUserUpdateEntity(updateRequest, result.Data);
        if (redisUserUpdateData.ChangeType is TokenType.Unknown)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid input data");

        if (redisUserUpdateData.ChangeType is TokenType.RoleChange)
            return await StatusCodes.Status400BadRequest.ResultState("Cannot request role update");

        if (!_userManager.AddUserUpdateDataToRedis(redisUserUpdateData))
            return await StatusCodes.Status400BadRequest.ResultState("Invalid input data");

        var cfmToken = _ctManager.CreateToken(result.Data.Id, redisUserUpdateData.ChangeType);
        var cfmLink = MailConfig.GetConfirmationLink(cfmToken.Value, cfmToken.TokenType);

        var sendMessageError = await _messageSenderManager
            .SendMessageAsync(redisUserUpdateData.ToUserUpdateMessage(result.Data, cfmLink));

        var cfmTokenResponse = new ReSendCfmTokenResponse
        {
            UserId = cfmToken.UserId,
            TokenType = cfmToken.TokenType
        };

        return string.IsNullOrEmpty(sendMessageError)
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
        _ = await _messageSenderManager.SendMessageAsync(new UserUpdateMessage
        {
            UserEmail = user.Email,
            UserName = user.Username,
            ChangeType = cfmToken.TokenType,
            ConfirmationLink = cfmLink
        });

        return await StatusCodes.Status200OK.ResultState();
    }

    #endregion
}