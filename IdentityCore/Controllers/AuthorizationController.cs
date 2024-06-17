using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Helpers;

namespace IdentityCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorizationController() : ControllerBase
{
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        return await StatusCodes.Status200OK.ResultState();
    }

    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole()
    {
        return await StatusCodes.Status200OK.ResultState();
    }
}