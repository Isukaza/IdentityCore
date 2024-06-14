using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

using Helpers;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Repository;
using IdentityCore.Managers;
using IdentityCore.Models;
using IdentityCore.Models.Request;
using IdentityCore.Models.Response;

namespace IdentityCore.Controllers;

public class UserController : Controller
{
    private UserRepository UserRepo => HttpContext.RequestServices.GetService<UserRepository>();
    private UserManager UserManager => HttpContext.RequestServices.GetService<UserManager>();

    [HttpGet("{useId:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUser(Guid useId)
    {
        if (useId == Guid.Empty)
            return await StatusCodes.Status400BadRequest.ResultState("Incorrect guid");
        
        var user = await UserRepo.GetUserByIdAsync(useId);
        
        return user != null
            ? await StatusCodes.Status200OK.ResultState("User info", user.ToUserResponse())
            : await StatusCodes.Status404NotFound.ResultState($"User by id:{useId} not found");
    }

    [HttpPut("create")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUser([FromBody] UserRequest userRequest)
    {
        if (userRequest is null)
            return await StatusCodes.Status400BadRequest.ResultState("Empty request or incorrect json");
        
        if (userRequest.Id != Guid.Empty)
            return await StatusCodes.Status400BadRequest.ResultState("Incorrect ID");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userResponse = await UserManager.CreateUser(userRequest);

        return await StatusCodes.Status201Created.ResultState("User created", userResponse.Id);
    }

    [HttpPut("update")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> UpdateUser([FromBody] UserRequest userRequest)
    {
        #region Validation
        
        if (userRequest is null)
            return await StatusCodes.Status400BadRequest.ResultState("Empty request or incorrect json");

        if (userRequest.Id == Guid.Empty)
            return await StatusCodes.Status400BadRequest.ResultState("Incorrect ID");

        var user = await UserRepo.GetUserByIdAsync(userRequest.Id);
        if (user is null)
            return await StatusCodes.Status404NotFound.ResultState("User not found");

        if (!ModelState.IsValid)
        {
            var hasAtLeastOneValidProperty = typeof(UserRequest)
                .GetProperties()
                .Any(prop =>
                {
                    var value = prop.GetValue(userRequest);
                    var validationContext = new ValidationContext(userRequest) { MemberName = prop.Name };
                    return prop.Name != "Id"
                           && value != null
                           && Validator.TryValidateProperty(value, validationContext, null);
                });

            if (!hasAtLeastOneValidProperty)
                return await StatusCodes.Status400BadRequest
                    .ResultState("At least one property must be specified correctly");
        }
        #endregion
        
        var result = await UserManager.UpdateUser(userRequest, user);
        
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
        
        var user = await UserRepo.GetUserByIdAsync(userId);
        if (user is null)
            return await StatusCodes.Status404NotFound.ResultState("User not found");
        
        return await UserManager.DeleteUserAsync(user)
            ? await StatusCodes.Status200OK.ResultState("User deleted")
            : await StatusCodes.Status500InternalServerError.ResultState("Error when deleting user");
    }

    #region TestMethods

    #if DEBUG
    
    [HttpGet("salt/{size:int}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenSalt(int size)
    {
        if (size < 16 || size > 64)
            return await StatusCodes.Status400BadRequest
                .ResultState("The salt size must be greater than 16 and less than 64 characters");

        var salt = UserHelper.GetSalt(size);
        
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
        var result = await UserManager.AddTestUsersToTheDatabase(users);
        
        return result
            ? await StatusCodes.Status200OK.ResultState("Password hash", users)
            : await StatusCodes.Status400BadRequest.ResultState("An error occurred while adding users");
    }
    
#endif

    #endregion
}