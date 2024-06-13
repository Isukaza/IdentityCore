using Helpers;
using Microsoft.AspNetCore.Mvc;

namespace IdentityCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController() : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login()
    {
        return await StatusCodes.Status200OK.ResultState();
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        return await StatusCodes.Status200OK.ResultState();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register()
    {
        return await StatusCodes.Status200OK.ResultState();
    }
}