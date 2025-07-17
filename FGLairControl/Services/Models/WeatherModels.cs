using System.Text.Json.Serialization;

namespace FGLairControl.Services.Models;

/// <summary>
/// Geocoding response from Open-Meteo API
/// </summary>
public class GeocodingResponse
{
    [JsonPropertyName("results")]
    public List<GeocodingResult>? Results { get; set; }
}

/// <summary>
/// Geocoding result for a location
/// </summary>
public class GeocodingResult
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }
    
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
    
    [JsonPropertyName("country")]
    public string? Country { get; set; }
    
    [JsonPropertyName("admin1")]
    public string? Admin1 { get; set; }
}

/// <summary>
/// Weather data response from Open-Meteo API
/// </summary>
public class WeatherResponse
{
    [JsonPropertyName("current")]
    public CurrentWeather? Current { get; set; }
}

/// <summary>
/// Current weather conditions
/// </summary>
public class CurrentWeather
{
    [JsonPropertyName("time")]
    public string? Time { get; set; }
    
    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; set; }
}

/// <summary>
/// Weather data for the application
/// </summary>
public class WeatherData
{
    public double TemperatureCelsius { get; set; }
    public DateTime Timestamp { get; set; }
}