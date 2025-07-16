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

    // Open-Meteo API URLs (free, no API key required)
    private const string WeatherBaseUrl = "https://api.open-meteo.com/v1/forecast";
    private const string GeocodingBaseUrl = "https://geocoding-api.open-meteo.com/v1/search";

    // Cache coordinates to avoid repeated geocoding
    private (double Latitude, double Longitude)? _cachedCoordinates;
    private string? _cachedCity;

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
        if (string.IsNullOrWhiteSpace(_settings.WeatherCity))
        {
            throw new InvalidOperationException("Weather city must be configured");
        }

        var coordinates = await GetCoordinatesAsync(_settings.WeatherCity, cancellationToken);
        
        _logger.LogInformation("Fetching weather data for {City} at coordinates: {Latitude}, {Longitude}", 
            _settings.WeatherCity, coordinates.Latitude, coordinates.Longitude);

        // Build Open-Meteo API URL with current weather parameters
        var url = $"{WeatherBaseUrl}?latitude={coordinates.Latitude:F4}&longitude={coordinates.Longitude:F4}&current=temperature_2m&timezone=auto";

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

            _logger.LogInformation("Current temperature in {City}: {Temperature}°C", _settings.WeatherCity, weatherData.TemperatureCelsius);
            return weatherData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch weather data from Open-Meteo API - network may be unavailable");
            throw new InvalidOperationException("Unable to retrieve weather data - check network connection", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Weather API request timed out");
            throw new InvalidOperationException("Weather API request timed out", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse weather response from Open-Meteo API");
            throw new InvalidOperationException("Invalid weather data format", ex);
        }
    }

    /// <summary>
    /// Get coordinates for a city using geocoding API
    /// </summary>
    private async Task<(double Latitude, double Longitude)> GetCoordinatesAsync(string city, CancellationToken cancellationToken)
    {
        // Return cached coordinates if city hasn't changed
        if (_cachedCoordinates.HasValue && string.Equals(_cachedCity, city, StringComparison.OrdinalIgnoreCase))
        {
            return _cachedCoordinates.Value;
        }

        _logger.LogInformation("Geocoding city: {City}", city);

        var encodedCity = Uri.EscapeDataString(city);
        var url = $"{GeocodingBaseUrl}?name={encodedCity}&count=1&language=en&format=json";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var geocodingResponse = await response.Content.ReadFromJsonAsync<GeocodingResponse>(_jsonOptions, cancellationToken);
            
            if (geocodingResponse?.Results == null || geocodingResponse.Results.Count == 0)
            {
                throw new InvalidOperationException($"City '{city}' not found. Please check the spelling and try again.");
            }

            var result = geocodingResponse.Results[0];
            var coordinates = (result.Latitude, result.Longitude);
            
            // Cache the result
            _cachedCoordinates = coordinates;
            _cachedCity = city;

            _logger.LogInformation("Found coordinates for {City}: {Latitude}, {Longitude} (Country: {Country})", 
                city, coordinates.Latitude, coordinates.Longitude, result.Country);

            return coordinates;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to geocode city from Open-Meteo API - network may be unavailable");
            throw new InvalidOperationException($"Unable to find location for '{city}' - check network connection", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Geocoding API request timed out");
            throw new InvalidOperationException("Geocoding request timed out", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse geocoding response from Open-Meteo API");
            throw new InvalidOperationException("Invalid geocoding data format", ex);
        }
    }
}