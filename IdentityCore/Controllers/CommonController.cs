using Helpers;
using IdentityCore.DAL.PostgreSQL.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityCore.Controllers;

[ApiController]
[Route("/[controller]")]
[Authorize(Roles = $"{nameof(UserRole.SuperAdmin)}, {nameof(UserRole.Admin)}")]
public class CommonController : Controller
{
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