using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

using Helpers;
using IdentityCore.Configuration;
using IdentityCore.DAL.Repository;
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

    private readonly UserRepository _userRepo;
    private readonly UserManager _userManager;
    private readonly MailManager _mailManager;
    private readonly ConfirmationRegistrationManager _crManager;

    public UserController(
        UserRepository userRepository,
        UserManager userManager,
        MailManager mailManager,
        ConfirmationRegistrationManager crManager)
    {
        _userRepo = userRepository;
        _userManager = userManager;
        _mailManager = mailManager;
        _crManager = crManager;
    }

    #endregion

    #region CRUD
    
    [HttpGet("{useId:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUser(Guid useId)
    {
        if (useId == Guid.Empty)
            return await StatusCodes.Status400BadRequest.ResultState("Incorrect guid");

        var user = await _userRepo.GetUserByIdAsync(useId);

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
        
        var userResponse = await _userManager.CreateUser(userCreateRequest);

        if (userResponse is null || userResponse.RegistrationTokens is null)
            return await StatusCodes.Status500InternalServerError.ResultState("Error creating user");

        var confirmationLink = Mail.Const.GetConfirmationLink(userResponse.RegistrationTokens.RegToken);
        
        var sendMailResult = string.Empty; /*await _mailManager.SendEmailAsync(
            Mail.Configs.Mail,
            userResponse.Email,
            Mail.Const.Subject,
            Mail.Const.GetHtmlContent(userResponse.Username, confirmationLink));*/
        
        Console.WriteLine(confirmationLink);
        
        return string.IsNullOrEmpty(sendMailResult)
            ? await StatusCodes.Status201Created.ResultState("User created")
            : await StatusCodes.Status400BadRequest.ResultState("Not send mail");
    }
    
    [HttpGet("confirmation-registration")]
    public async Task<IActionResult> ConfirmationRegistrationUser([Required] string token)
    {
        if (!_crManager.IsTokenValid(token))
            return await StatusCodes.Status400BadRequest.ResultState("Invalid token");

        await _crManager.DeleteExpiredTokens();
        var result = await _crManager.ActivatedUser(token);
        
        return string.IsNullOrEmpty(result)
            ? await StatusCodes.Status200OK.ResultState()
            : await StatusCodes.Status400BadRequest.ResultState(result);
    }
    
    [HttpPost("resend-reg-token")]
    public async Task<IActionResult> ResendConfirmationRegistrationUser([FromBody] string username)
    {
        return await StatusCodes.Status200OK.ResultState();
    }

    [HttpPut("update")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> UpdateUser([FromBody] UserUpdateRequest updateRequest)
    {
        #region Validation

        if (string.IsNullOrEmpty(updateRequest.Username)
            && string.IsNullOrEmpty(updateRequest.Email)
            && string.IsNullOrEmpty(updateRequest.Password))
            return await StatusCodes.Status400BadRequest.ResultState("At least one field must be specified");

        var user = await _userRepo.GetUserByIdAsync(updateRequest.Id);
        if (user is null)
            return await StatusCodes.Status404NotFound.ResultState("User not found");

        #endregion

        var result = await _userManager.UpdateUser(updateRequest, user);

        if (result.Success)
            return await StatusCodes.Status200OK.ResultState("User updated", result.Data.ToUserResponse());

        return await StatusCodes.Status400BadRequest.ResultState(result.ErrorMessage);
    }

    [HttpDelete("delete")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        if (userId == Guid.Empty)
            return await StatusCodes.Status400BadRequest.ResultState("Incorrect ID");

        var user = await _userRepo.GetUserByIdAsync(userId);
        if (user is null)
            return await StatusCodes.Status404NotFound.ResultState("User not found");

        return await _userManager.DeleteUserAsync(user)
            ? await StatusCodes.Status200OK.ResultState("User deleted")
            : await StatusCodes.Status500InternalServerError.ResultState("Error when deleting user");
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
    public async Task<IActionResult> GetPasswordHash(int count)
    {
        if (count < 1 || count > 1000)
            return await StatusCodes.Status400BadRequest
                .ResultState("The number of users for generation cannot be less than 1 and more than 1000");

        var users = UserManager.GenerateUsers(count);

        return await StatusCodes.Status200OK.ResultState("Password hash", users);
    }

    [HttpPost("generate-test-database")]
    [ProducesResponseType(typeof(List<TestUserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPasswordHash(int count, string password)
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