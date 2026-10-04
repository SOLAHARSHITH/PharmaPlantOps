using Microsoft.AspNetCore.Authorization;
using PlantOps.Api.Contracts;
using PlantOps.Api.Services;

namespace PlantOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/weather")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherClient _weatherClient;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(IWeatherClient weatherClient, ILogger<WeatherController> logger)
    {
        _weatherClient = weatherClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<WeatherResponse>> Get(CancellationToken cancellationToken)
    {
        try
        {
            var weather = await _weatherClient.GetCurrentAsync(cancellationToken);
            return Ok(new WeatherResponse(weather.Location, weather.TemperatureCelsius, weather.WeatherCode, weather.Condition));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open-Meteo request failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Weather is unavailable." });
        }
    }
}
