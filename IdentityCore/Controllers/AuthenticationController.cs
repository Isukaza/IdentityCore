using Helpers;
using Microsoft.AspNetCore.Mvc;

namespace IdentityCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(ILogger<AuthenticationController> logger)
    {
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login()
    {
        return await StatusCodes.Status200OK.ResultStateAsync(HttpContext, _logger);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        return await StatusCodes.Status200OK.ResultStateAsync(HttpContext, _logger);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register()
    {
        return await StatusCodes.Status200OK.ResultStateAsync(HttpContext, _logger);
    }
}