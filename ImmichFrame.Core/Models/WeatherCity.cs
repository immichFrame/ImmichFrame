namespace ImmichFrame.Core.Models;

public record WeatherCity(int Id, string Name, string Country, double Latitude, double Longitude)
{
    // YAML configuration needs a parameterless constructor.
    public WeatherCity() : this(0, string.Empty, string.Empty, 0, 0) { }
}
