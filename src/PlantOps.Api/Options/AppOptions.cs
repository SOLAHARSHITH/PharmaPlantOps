namespace PlantOps.Api.Options;

public class AuthOptions
{
    public string JwtIssuer { get; set; } = "PlantOps";
    public string JwtAudience { get; set; } = "PlantOps";
    public string JwtSigningKey { get; set; } = "";
    public int TokenHours { get; set; } = 8;
}

public class SimulatorOptions
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 15;
}

public class WeatherOptions
{
    public double Latitude { get; set; } = 40.029;
    public double Longitude { get; set; } = -75.620;
    public string LocationName { get; set; } = "Exton, PA";
}
