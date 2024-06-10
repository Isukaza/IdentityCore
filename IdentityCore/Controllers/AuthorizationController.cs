using Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorizationController : ControllerBase
{
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(ILogger<AuthorizationController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        return await StatusCodes.Status200OK.ResultStateAsync(HttpContext, _logger);
    }

    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole()
    {
        return await StatusCodes.Status200OK.ResultStateAsync(HttpContext, _logger);
    }
}