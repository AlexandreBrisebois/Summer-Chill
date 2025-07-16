using System.Net.Http.Json;
using System.Text.Json;
using FGLairControl.Services.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FGLairControl.Services;

/// <summary>
/// Weather service implementation using Open-Meteo free API
/// </summary>
public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private readonly FGLairSettings _settings;
    private readonly JsonSerializerOptions _jsonOptions;

    // Open-Meteo API base URL (free, no API key required)
    private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";

    public WeatherService(
        HttpClient httpClient,
        IOptions<FGLairSettings> settings,
        ILogger<WeatherService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task<WeatherData> GetCurrentWeatherAsync(CancellationToken cancellationToken = default)
    {
        if (_settings.WeatherLatitude == 0 || _settings.WeatherLongitude == 0)
        {
            throw new InvalidOperationException("Weather location (latitude/longitude) must be configured");
        }

        _logger.LogInformation("Fetching weather data for location: {Latitude}, {Longitude}", 
            _settings.WeatherLatitude, _settings.WeatherLongitude);

        // Build Open-Meteo API URL with current weather parameters
        var url = $"{BaseUrl}?latitude={_settings.WeatherLatitude:F4}&longitude={_settings.WeatherLongitude:F4}&current=temperature_2m&timezone=auto";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var weatherResponse = await response.Content.ReadFromJsonAsync<WeatherResponse>(_jsonOptions, cancellationToken);
            
            if (weatherResponse?.Current == null)
            {
                throw new InvalidOperationException("Invalid weather response received");
            }

            var weatherData = new WeatherData
            {
                TemperatureCelsius = weatherResponse.Current.Temperature,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Current temperature: {Temperature}°C", weatherData.TemperatureCelsius);
            return weatherData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch weather data from Open-Meteo API");
            throw new InvalidOperationException("Unable to retrieve weather data", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse weather response from Open-Meteo API");
            throw new InvalidOperationException("Invalid weather data format", ex);
        }
    }
}