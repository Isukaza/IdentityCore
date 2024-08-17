using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.Models;
using IdentityCore.Managers;
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

    private readonly UserManager _userManager;
    private readonly MailManager _mailManager;
    private readonly ConfirmationTokenManager _ctManager;

    public UserController(UserManager userManager, MailManager mailManager, ConfirmationTokenManager ctManager)
    {
        _userManager = userManager;
        _mailManager = mailManager;
        _ctManager = ctManager;
    }

    #endregion

    #region CRUD

    [HttpGet("{useId:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUser(Guid useId)
    {
        var user = await _userManager.GetUserByIdAsync(useId);
        return user != null
            ? await StatusCodes.Status200OK.ResultState("User info", user.ToUserResponse())
            : await StatusCodes.Status404NotFound.ResultState($"User by id:{useId} not found");
    }

    [AllowAnonymous]
    [HttpPost("registration")]
    [ProducesResponseType(typeof(ReSendCfmTokenResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegistrationUser([FromBody] UserCreateRequest userCreateRequest)
    {
        var errorMessage = await _userManager.ValidateRegistration(userCreateRequest);
        if (!string.IsNullOrEmpty(errorMessage))
            return await StatusCodes.Status400BadRequest.ResultState(errorMessage);

        var user = _userManager.CreateUserForRegistration(userCreateRequest);
        if (user is null)
            return await StatusCodes.Status500InternalServerError.ResultState("Error creating user");

        var cfmToken = _ctManager.CreateConfirmationToken(user.Id, TokenType.RegistrationConfirmation);
        if (cfmToken is null)
        {
            _ = await _userManager.DeleteRegisteredUserFromRedisAsync(user);
            return await StatusCodes.Status500InternalServerError.ResultState("Error creating user");
        }

        var cfmLink = MailConfig.GetConfirmationLink(cfmToken.Value, cfmToken.TokenType);
        var sendMailError = await _mailManager.SendEmailAsync(user.Email, cfmToken.TokenType, cfmLink, user);
        return string.IsNullOrEmpty(sendMailError)
            ? await StatusCodes.Status201Created
                .ResultState("Awaiting confirmation by mail", new ReSendCfmTokenResponse
                {
                    UserId = cfmToken.UserId,
                    TokenType = cfmToken.TokenType
                })
            : await StatusCodes.Status400BadRequest.ResultState("Not send mail");
    }

    [HttpPut("update")]
    [ProducesResponseType(typeof(ReSendCfmTokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUser([FromBody] UserUpdateRequest updateRequest)
    {
        if (await _userManager.IsUserUpdateInProgress(updateRequest.Id))
            return await StatusCodes.Status429TooManyRequests.ResultState("Complete the current update process");

        var result = await _userManager.ValidateUserUpdateAsync(updateRequest);
        if (!result.Success)
            return await StatusCodes.Status400BadRequest.ResultState(result.ErrorMessage);

        var tokenType = UserManager.DetermineConfirmationTokenType(updateRequest);
        if (tokenType is TokenType.Unknown)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid input data");

        var redisUserUpdate = _userManager.SaveUserUpdateToRedisAsync(updateRequest, tokenType);
        if (redisUserUpdate is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid input data");

        var cfmToken = _ctManager.CreateConfirmationToken(result.Data.Id, tokenType);
        var cfmLink = MailConfig.GetConfirmationLink(cfmToken.Value, cfmToken.TokenType);
        var sendMailError = await _mailManager
            .SendEmailAsync(result.Data.Email, tokenType, cfmLink, result.Data, redisUserUpdate);

        return string.IsNullOrEmpty(sendMailError)
            ? await StatusCodes.Status200OK
                .ResultState("Awaiting confirmation by mail", new ReSendCfmTokenResponse
                {
                    UserId = cfmToken.UserId,
                    TokenType = cfmToken.TokenType
                })
            : await StatusCodes.Status500InternalServerError.ResultState("Not send mail");
    }

    [HttpGet("send-new-email-confirmation")]
    [ProducesResponseType(typeof(ReSendCfmTokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendNewEmailConfirmation([Required] Guid userId)
    {
        var tokenDb = await _ctManager.GetTokenByUserIdAsync(userId, TokenType.EmailChangeNew);
        if (tokenDb is null || tokenDb.UserId != userId)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var user = await _userManager.GetUserByIdAsync(userId);
        if (user is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var userUpdate = await _userManager.GetUpdateUserFromRedisByIdAsync(tokenDb.UserId);
        if (userUpdate is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var cfmLink = MailConfig.GetConfirmationLink(tokenDb.Value, tokenDb.TokenType);
        var sendMailError = await _mailManager
            .SendEmailAsync(userUpdate.Email, tokenDb.TokenType, cfmLink, user, userUpdate);

        return string.IsNullOrEmpty(sendMailError)
            ? await StatusCodes.Status200OK
                .ResultState("Confirmation email sent to the new email address", new ReSendCfmTokenResponse
                {
                    UserId = tokenDb.UserId,
                    TokenType = TokenType.EmailChangeNew
                })
            : await StatusCodes.Status500InternalServerError.ResultState(sendMailError);
    }

    [HttpDelete("delete")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        var user = await _userManager.GetUserByIdAsync(userId);
        if (user is null)
            return await StatusCodes.Status404NotFound.ResultState("User not found");

        return await _userManager.DeleteUserAsync(user)
            ? await StatusCodes.Status200OK.ResultState("User deleted")
            : await StatusCodes.Status500InternalServerError.ResultState("Error when deleting user");
    }

    #endregion

    #region Confirmation action

    [AllowAnonymous]
    [HttpGet("cfm-reg-token")]
    public async Task<IActionResult> ConfirmationRegToken([FromQuery] CfmTokenRequest tokenRequest)
    {
        return await HandleTokenConfirmation(tokenRequest, true);
    }

    [HttpGet("cfm-token")]
    public async Task<IActionResult> ConfirmationToken([FromQuery] CfmTokenRequest tokenRequest)
    {
        return await HandleTokenConfirmation(tokenRequest, false);
    }

    [AllowAnonymous]
    [HttpPost("resend-cfm-reg-token")]
    public async Task<IActionResult> ResendCfmRegToken(ReSendCfmTokenRequest tokenRequest)
    {
        return await HandleTokenResend(tokenRequest, true);
    }

    [HttpPost("resend-cfm-token")]
    public async Task<IActionResult> ResendCfmToken(ReSendCfmTokenRequest tokenRequest)
    {
        return await HandleTokenResend(tokenRequest, false);
    }

    private async Task<IActionResult> HandleTokenConfirmation(CfmTokenRequest tokenRequest, bool isRegistration)
    {
        if (!ConfirmationTokenManager.ValidateTokenTypeForRequest(tokenRequest.TokenType, isRegistration))
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var tokenDb = await _ctManager.GetTokenAsync(tokenRequest.Token, tokenRequest.TokenType);
        if (tokenDb is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var user = tokenDb.TokenType == TokenType.RegistrationConfirmation
            ? await _userManager.GetRegUserFromRedisByIdAsync(tokenDb.UserId)
            : await _userManager.GetUserByIdAsync(tokenDb.UserId);
        if (user is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var userUpdateData = await _userManager.GetUpdateUserFromRedisByIdAsync(tokenDb.UserId);
        if (userUpdateData is null && tokenDb.TokenType != TokenType.RegistrationConfirmation)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var executionError = await _userManager.ProcessUserTokenAction(user, userUpdateData, tokenDb);
        return string.IsNullOrEmpty(executionError)
            ? await StatusCodes.Status200OK.ResultState("Operation was successfully completed")
            : await StatusCodes.Status500InternalServerError.ResultState(executionError);
    }

    private async Task<IActionResult> HandleTokenResend(ReSendCfmTokenRequest tokenRequest, bool isRegistration)
    {
        if (!ConfirmationTokenManager.ValidateTokenTypeForRequest(tokenRequest.TokenType, isRegistration))
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var tokenDb = await _ctManager.GetTokenByUserIdAsync(tokenRequest.UserId, tokenRequest.TokenType);
        if (tokenDb is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var timeForNextAttempt = ConfirmationTokenManager.GetNextAttemptTime(tokenDb);
        if (!string.IsNullOrEmpty(timeForNextAttempt))
            return await StatusCodes.Status429TooManyRequests.ResultState(timeForNextAttempt);

        var user = tokenDb.TokenType == TokenType.RegistrationConfirmation
            ? await _userManager.GetRegUserFromRedisByIdAsync(tokenDb.UserId)
            : await _userManager.GetUserByIdAsync(tokenDb.UserId);
        if (user is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var userUpdate = tokenDb.TokenType != TokenType.RegistrationConfirmation
            ? await _userManager.GetUpdateUserFromRedisByIdAsync(tokenDb.UserId)
            : null;
        if (tokenDb.TokenType != TokenType.RegistrationConfirmation && userUpdate == null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var updatedToken = await _ctManager.UpdateCfmToken(tokenDb, user, userUpdate);
        if (updatedToken is null)
            return await StatusCodes.Status500InternalServerError
                .ResultState("Failed to send verification token");

        var cfmLink = MailConfig.GetConfirmationLink(updatedToken.Value, updatedToken.TokenType);
        var email = tokenDb.TokenType == TokenType.EmailChangeNew && userUpdate is not null
            ? userUpdate.Email
            : user.Email;
        
        var sendMailError = await _mailManager.SendEmailAsync(email, tokenDb.TokenType, cfmLink, user, userUpdate);
        return string.IsNullOrEmpty(sendMailError)
            ? await StatusCodes.Status200OK.ResultState("Operation was successfully completed")
            : await StatusCodes.Status400BadRequest.ResultState("Not send mail");
    }

    #endregion

    #region TestMethods

#if DEBUG

    [HttpGet("salt/{size:int}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenSalt(int size)
    {
        if (size < 16 || size > 64)
            return await StatusCodes.Status400BadRequest
                .ResultState("The salt size must be greater than 16 and less than 64 characters");

        var salt = UserHelper.GenerateSalt(size);

        return await StatusCodes.Status200OK.ResultState("Salt", salt);
    }

    [HttpGet("password/{size:int}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenPassword(int size)
    {
        if (size < 12 || size > 64)
            return await StatusCodes.Status400BadRequest
                .ResultState("The password size must be greater than 12 and less than 64 characters");

        var pass = UserHelper.GeneratePassword(size);

        return await StatusCodes.Status200OK.ResultState("Password", pass);
    }

    [HttpGet("password-hash")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPasswordHash(string password, string salt)
    {
        if (password.Length < 12 || password.Length > 64)
            return await StatusCodes.Status400BadRequest
                .ResultState("The password size must be greater than 12 and less than 64 characters");

        if (salt.Length < 12 || salt.Length > 64)
            return await StatusCodes.Status400BadRequest
                .ResultState("The salt size must be greater than 16 and less than 64 characters");

        var passwordHash = UserHelper.GetPasswordHash(password, salt);

        return await StatusCodes.Status200OK.ResultState("Password hash", passwordHash);
    }

    [HttpGet("gen-users")]
    [ProducesResponseType(typeof(List<TestUserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateUser(int count)
    {
        if (count < 1 || count > 1000)
            return await StatusCodes.Status400BadRequest
                .ResultState("The number of users for generation cannot be less than 1 and more than 1000");

        var users = UserManager.GenerateUsers(count);

        return await StatusCodes.Status200OK.ResultState("Password hash", users);
    }

    [HttpPost("generate-test-database")]
    [ProducesResponseType(typeof(List<TestUserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateTestDb(int count, string password)
    {
        if (count < 1 || count > 1000)
            return await StatusCodes.Status400BadRequest
                .ResultState("The number of users for generation cannot be less than 1 and more than 1000");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || password.Length > 64)
            return await StatusCodes.Status400BadRequest
                .ResultState("The password must be between 12 and 64 characters long");

        var users = UserManager.GenerateUsers(count, password);
        var result = await _userManager.AddTestUsersToTheDatabase(users);

        return result
            ? await StatusCodes.Status200OK.ResultState("Password hash", users)
            : await StatusCodes.Status400BadRequest.ResultState("An error occurred while adding users");
    }

#endif

    #endregion
}