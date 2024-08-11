using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

using Helpers;
using Helpers.ValidationAttributes;
using IdentityCore.Configuration;
using IdentityCore.DAL.Models;
using IdentityCore.Managers;
using IdentityCore.Models;
using IdentityCore.Models.Request;
using IdentityCore.Models.Response;

namespace IdentityCore.Controllers;

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

    [HttpPost("registration")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
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
        var sendMailError = await _mailManager.SendEmailAsync(
            MailConfig.Values.Mail,
            user.Email,
            cfmToken.TokenType,
            cfmLink,
            user);

        return string.IsNullOrEmpty(sendMailError)
            ? await StatusCodes.Status201Created.ResultState(user.Id.ToString())
            : await StatusCodes.Status400BadRequest.ResultState("Not send mail");
    }

    [HttpPut("update")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
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
        var sendMailError = await _mailManager.SendEmailAsync(
            MailConfig.Values.Mail,
            result.Data.Email,
            tokenType,
            cfmLink,
            result.Data,
            redisUserUpdate);

        return string.IsNullOrEmpty(sendMailError)
            ? await StatusCodes.Status200OK.ResultState("Awaiting confirmation by mail")
            : await StatusCodes.Status500InternalServerError.ResultState("Not send mail");
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

    [HttpGet("cfm-token")]
    public async Task<IActionResult> ConfirmationToken(
        [Required] [ValidToken] string token,
        [Required] TokenType tokenType)
    {
        var tokenDb = await _ctManager.GetTokenAsync(token, tokenType);
        if (tokenDb is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        var user = await _userManager.GetUserFromRedisByIdAsync(tokenDb.UserId, tokenType);
        if (user is null)
        {
            _ = await _ctManager.DeleteToken(tokenDb);
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");
        }

        var userActivationError = await _userManager.ActivatedUser(user, tokenDb);
        return string.IsNullOrEmpty(userActivationError)
            ? await StatusCodes.Status200OK.ResultState("Operation was successfully completed")
            : await StatusCodes.Status500InternalServerError.ResultState(userActivationError);
    }

    [HttpPost("resend-cfm-token")]
    public async Task<IActionResult> ResendCfmToken([FromBody] ResendConfirmationEmailRequest emailRequest)
    {
        /*var result = await _ctManager.ValidateResendConfirmationRegistrationMail(emailRequest);
        if (!result.Success)
            return await StatusCodes.Status400BadRequest.ResultState(result.ErrorMessage);

        var user = await _userManager.GetUserFromRedisByIdAsync(result.Data.UserId);
        if (user is null)
            return await StatusCodes.Status400BadRequest.ResultState("Invalid input data");

        var timeForNextAttempt = ConfirmationTokenManager.GetNextAttemptTime(result.Data);
        if (!string.IsNullOrEmpty(timeForNextAttempt))
            return await StatusCodes.Status429TooManyRequests.ResultState(timeForNextAttempt);

        var updatedToken = await _ctManager.UpdateRegistrationToken(result.Data, result.Data.UserId);
        if (updatedToken is null)
            return await StatusCodes.Status500InternalServerError
                .ResultState("Failed to send verification token");

        var confirmationLink = Mail.GetConfirmationLink(updatedToken.Value, TokenType.RegistrationConfirmation);
        var sendMailError = await _mailManager.SendEmailAsync(
            Mail.Configs.Mail,
            user.Email,
            updatedToken.TokenType,
            confirmationLink,
            null,
            user.Username);

        return string.IsNullOrEmpty(sendMailError)
            ? await StatusCodes.Status200OK.ResultState("Operation was successfully completed")
            : await StatusCodes.Status400BadRequest.ResultState("Not send mail");*/
        
        return await StatusCodes.Status500InternalServerError.ResultState();
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