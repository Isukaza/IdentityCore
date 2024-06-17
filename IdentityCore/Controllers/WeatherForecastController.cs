using Microsoft.AspNetCore.Mvc;
using Helpers;
using IdentityCore.Models.Response;

namespace IdentityCore.Controllers;

//[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController() : ControllerBase
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IActionResult> Get()
    {
        var weatherForecast = Enumerable.Range(1, 5).Select(index => new WeatherForecastResponse
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        
        return await StatusCodes.Status200OK.ResultState("Curren weather forecast", weatherForecast);
    }
}