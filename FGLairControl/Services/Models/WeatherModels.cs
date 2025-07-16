using System.Text.Json.Serialization;

namespace FGLairControl.Services.Models;

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