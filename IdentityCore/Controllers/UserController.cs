using Microsoft.AspNetCore.Mvc;

using Helpers;
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

    public UserController(UserRepository userRepository, UserManager userManager)
    {
        _userRepo = userRepository;
        _userManager = userManager;
    }

    #endregion

    #region GET
    
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

    #endregion

    #region CRUD

    [HttpPut("create")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest userCreateRequest)
    {
        var userResponse = await _userManager.CreateUser(userCreateRequest);
        return await StatusCodes.Status201Created.ResultState("User created", userResponse.Id);
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