using Microsoft.AspNetCore.Mvc;

using Helpers;
using IdentityCore.Managers;

namespace IdentityCore.Controllers;

public class UserController : Controller
{
    #region TestMethods

    #if DEBUG
    
    [HttpGet("salt/{size:int}")]
    public async Task<IActionResult> GenSalt(int size)
    {
        if (size < 16 || size > 64)
            return await StatusCodes.Status400BadRequest
                .ResultState("The salt size must be greater than 16 and less than 64 characters");

        var salt = UserHelper.GetSalt(size);
        
        return await StatusCodes.Status200OK.ResultState("Salt", salt);
    }
    
    [HttpGet("password/{size:int}")]
    public async Task<IActionResult> GenPassword(int size)
    {
        if (size < 12 || size > 64)
            return await StatusCodes.Status400BadRequest
                .ResultState("The password size must be greater than 12 and less than 64 characters");

        var pass = UserHelper.GeneratePassword(size);
        
        return await StatusCodes.Status200OK.ResultState("Password", pass);
    }
    
    [HttpGet("password-hash")]
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
    public async Task<IActionResult> GetPasswordHash(int count)
    {
        if (count < 1 || count > 1000)
            return await StatusCodes.Status400BadRequest
                .ResultState("The number of users for generation cannot be less than 1 and more than 1000");

        var users = UserManager.GenerateUsers(count);
        
        return await StatusCodes.Status200OK.ResultState("Password hash", users);
    }
    
#endif

    #endregion
}