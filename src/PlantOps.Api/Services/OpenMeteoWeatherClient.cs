using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PlantOps.Api.Options;

namespace PlantOps.Api.Services;

public interface IWeatherClient
{
    Task<WeatherSnapshot> GetCurrentAsync(CancellationToken cancellationToken);
}

public record WeatherSnapshot(string Location, double TemperatureCelsius, int WeatherCode, string Condition);

public class OpenMeteoWeatherClient : IWeatherClient
{
    private readonly HttpClient _httpClient;
    private readonly WeatherOptions _options;

    public OpenMeteoWeatherClient(HttpClient httpClient, IOptions<WeatherOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<WeatherSnapshot> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var latitude = _options.Latitude.ToString(CultureInfo.InvariantCulture);
        var longitude = _options.Longitude.ToString(CultureInfo.InvariantCulture);
        var path = $"v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,weather_code";
        using var response = await _httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("current", out var current))
        {
            throw new InvalidOperationException("Open-Meteo response did not include current conditions.");
        }

        var temperature = current.GetProperty("temperature_2m").GetDouble();
        var weatherCode = current.GetProperty("weather_code").GetInt32();
        return new WeatherSnapshot(_options.LocationName, temperature, weatherCode, Describe(weatherCode));
    }

    public static string Describe(int weatherCode) => weatherCode switch
    {
        0 => "Clear",
        1 or 2 => "Partly cloudy",
        3 => "Cloudy",
        45 or 48 => "Fog",
        51 or 53 or 55 or 56 or 57 => "Drizzle",
        61 or 63 or 65 or 66 or 67 => "Rain",
        71 or 73 or 75 or 77 => "Snow",
        80 or 81 or 82 => "Showers",
        95 or 96 or 99 => "Thunderstorm",
        _ => "Unknown"
    };
}
