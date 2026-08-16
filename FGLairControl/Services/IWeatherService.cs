using FGLairControl.Services.Models;

namespace FGLairControl.Services;

/// <summary>
/// Interface for weather data retrieval
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// Gets the current outside temperature for the configured location
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Current weather data including temperature</returns>
    Task<WeatherData> GetCurrentWeatherAsync(CancellationToken cancellationToken = default);
}