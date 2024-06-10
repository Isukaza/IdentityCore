using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Helpers;

public static class ControllerHelper
{
    private static readonly List<int> NoContentCodes = [StatusCodes.Status204NoContent];

    public static async Task<ActionResult> ResultStateAsync(this int code,
        HttpContext context,
        ILogger logger,
        string message = "",
        object? value = null)
    {
        if (code < 400)
            logger.LogInformation("[{Code}] {Message}", code, message);
        else
            logger.LogError("[{Code}] {Message}", code, message);
        
        if (NoContentCodes.Contains(code))
            return new NoContentResult();

        return new ObjectResult(value ?? message)
        {
            StatusCode = code
        };
    }
}