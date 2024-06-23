using Microsoft.AspNetCore.Mvc;

using Helpers;
using IdentityCore.DAL.Repository;
using IdentityCore.Managers;

namespace IdentityCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    #region C-tor and fields

    private readonly UserRepository _userRepo;
    private readonly UserManager _userManager;

    public AuthenticationController(UserRepository userRepository, UserManager userManager)
    {
        _userRepo = userRepository;
        _userManager = userManager;
    }

    #endregion
    
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        return await StatusCodes.Status200OK.ResultState();
    }
}