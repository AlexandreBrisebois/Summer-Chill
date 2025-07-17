using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FGLairControl.Services.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FGLairControl.Services;

/// <summary>
/// Simple implementation of the FGLair API client based on REST API calls
/// </summary>
public class FGLairClient : IFGLairClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FGLairClient> _logger;
    private readonly FGLairSettings _settings;
    private string _accessToken = string.Empty;
    private string _refreshToken = string.Empty;
    private DateTime _tokenExpiryTime = DateTime.MinValue;
    private readonly JsonSerializerOptions _jsonOptions;

    // API endpoint constants based on rest.http examples
    private const string LoginEndpoint = "/users/sign_in.json";
    private const string RefreshEndpoint = "/users/refresh_token.json";
    private const string DevicePropertiesEndpoint = "/apiv1/dsns/{0}/properties";
    private const string SetPropertyEndpoint = "/apiv1/dsns/{0}/properties/{1}/datapoints";
    
    // Default temperature in case current temperature cannot be retrieved
    private const double DefaultTemperature = 22.0;
    
    public FGLairClient(
        HttpClient httpClient,
        IOptions<FGLairSettings> settings,
        ILogger<FGLairClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        // Configure HttpClient based on rest.http examples
        _httpClient.BaseAddress = new Uri(FGLairSettings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.UserAgent.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FGLAir", "2.0"));
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
    }

    /// <inheritdoc />
    public async Task LoginAsync(CancellationToken cancellationToken = default)
    {
        // Check if current token is still valid (with 5 minute buffer)
        if (!IsTokenExpiredOrExpiring())
        {
            _logger.LogDebug("Using existing access token (expires at {ExpiryTime})", _tokenExpiryTime);
            return;
        }

        // Try to refresh token if we have a refresh token
        if (!string.IsNullOrEmpty(_refreshToken))
        {
            _logger.LogInformation("Access token expired or expiring soon, attempting to refresh");
            if (await TryRefreshTokenAsync(cancellationToken))
            {
                return;
            }
            _logger.LogWarning("Token refresh failed, performing fresh login");
        }

        await PerformFreshLoginAsync(cancellationToken);
    }

    /// <summary>
    /// Performs a fresh login using username and password
    /// </summary>
    private async Task PerformFreshLoginAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logging into FGLair API with username {Username}", _settings.Username);

        // Create login request exactly as shown in rest.http
        var loginRequest = new
        {
            user = new
            {
                email = _settings.Username,
                password = _settings.Password,
                application = new
                {
                    app_id = FGLairSettings.AppId,
                    app_secret = FGLairSettings.AppSecret
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(LoginEndpoint, loginRequest, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        await ProcessAuthenticationResponseAsync(response, cancellationToken);
        _logger.LogInformation("Fresh login successful");
    }

    /// <summary>
    /// Attempts to refresh the access token using the refresh token
    /// </summary>
    private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var refreshRequest = new
            {
                user = new
                {
                    refresh_token = _refreshToken
                }
            };

            var response = await _httpClient.PostAsJsonAsync(RefreshEndpoint, refreshRequest, _jsonOptions, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token refresh failed with status {StatusCode}", response.StatusCode);
                return false;
            }

            await ProcessAuthenticationResponseAsync(response, cancellationToken);
            _logger.LogInformation("Token refresh successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception during token refresh");
            return false;
        }
    }

    /// <summary>
    /// Processes authentication response and extracts tokens
    /// </summary>
    private async Task ProcessAuthenticationResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseContent);
        
        if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
        {
            _accessToken = tokenElement.GetString() ?? throw new InvalidOperationException("No access token in response");
            
            // Extract refresh token if present
            if (doc.RootElement.TryGetProperty("refresh_token", out var refreshElement))
            {
                _refreshToken = refreshElement.GetString() ?? string.Empty;
            }
            
            // Calculate expiry time (default to 1 hour if not specified)
            var expiresInSeconds = 3600; // Default to 1 hour
            if (doc.RootElement.TryGetProperty("expires_in", out var expiresElement) && 
                expiresElement.TryGetInt32(out var expiry))
            {
                expiresInSeconds = expiry;
            }
            
            _tokenExpiryTime = DateTime.UtcNow.AddSeconds(expiresInSeconds);
            
            // Set authorization header for future requests
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("auth_token", _accessToken);
            
            _logger.LogDebug("Token processed - expires at {ExpiryTime}", _tokenExpiryTime);
        }
        else
        {
            throw new InvalidOperationException("Authentication failed - no access token received");
        }
    }

    /// <summary>
    /// Executes an HTTP request with automatic token refresh on 401 errors
    /// </summary>
    private async Task<HttpResponseMessage> ExecuteRequestWithRetryAsync(
        Func<Task<HttpResponseMessage>> requestFunc, 
        CancellationToken cancellationToken)
    {
        await LoginAsync(cancellationToken);
        
        var response = await requestFunc();
        
        // If we get a 401 Unauthorized, try to refresh token and retry once
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 Unauthorized, clearing token and attempting fresh login");
            
            // Clear current tokens to force refresh
            _accessToken = string.Empty;
            _refreshToken = string.Empty;
            _tokenExpiryTime = DateTime.MinValue;
            
            // Remove auth header to avoid using stale token
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            // Attempt fresh login
            await LoginAsync(cancellationToken);
            
            // Retry the request
            response.Dispose();
            response = await requestFunc();
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Request still returns 401 after token refresh - authentication may have failed");
            }
        }
        
        return response;
    }

    /// <summary>
    /// Checks if the current token is expired or about to expire
    /// </summary>
    private bool IsTokenExpiredOrExpiring()
    {
        return string.IsNullOrEmpty(_accessToken) || _tokenExpiryTime <= DateTime.UtcNow.AddMinutes(5);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        await LoginAsync(cancellationToken);
        
        _logger.LogInformation("Getting device properties for DSN: {DeviceDsn}", _settings.DeviceDsn);
        
        // Return the configured device as we're targeting a specific DSN
        return new List<DeviceInfo>
        {
            new DeviceInfo 
            { 
                Dsn = _settings.DeviceDsn, 
                Name = "AC", 
                Model = "FGLair Device" 
            }
        };
    }

    /// <inheritdoc />
    public async Task SendLouverCommandAsync(string position, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_settings.DeviceDsn))
        {
            throw new InvalidOperationException("Device DSN must be configured");
        }

        if (string.IsNullOrEmpty(position))
        {
            throw new InvalidOperationException("Louver position must be provided");
        }

        _logger.LogInformation("Setting vertical louver direction to {Position} for device {DeviceDsn}", 
            position, _settings.DeviceDsn);

        // Send command exactly as shown in rest.http
        var endpoint = string.Format(SetPropertyEndpoint, _settings.DeviceDsn, "af_vertical_direction");
        var request = new
        {
            datapoint = new
            {
                value = position
            }
        };

        var response = await ExecuteRequestWithRetryAsync(
            () => _httpClient.PostAsJsonAsync(endpoint, request, _jsonOptions, cancellationToken),
            cancellationToken);
        
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Louver command sent successfully");
    }

    /// <inheritdoc />
    public async Task<string> GetLouverPositionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_settings.DeviceDsn))
        {
            throw new InvalidOperationException("Device DSN must be configured");
        }

        // Get device properties exactly as shown in rest.http
        var endpoint = string.Format(DevicePropertiesEndpoint, _settings.DeviceDsn);
        var response = await ExecuteRequestWithRetryAsync(
            () => _httpClient.GetAsync(endpoint, cancellationToken),
            cancellationToken);
        
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseContent);

        // Parse the properties array to find af_vertical_direction
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var propertyElement in doc.RootElement.EnumerateArray())
            {
                if (propertyElement.TryGetProperty("property", out var propObj) &&
                    propObj.TryGetProperty("name", out var nameElement) &&
                    nameElement.GetString() == "af_vertical_direction" &&
                    propObj.TryGetProperty("value", out var valueElement))
                {
                    var value = valueElement.ValueKind switch
                    {
                        JsonValueKind.String => valueElement.GetString() ?? "0",
                        JsonValueKind.Number => valueElement.GetInt32().ToString(),
                        _ => "0"
                    };
                    
                    _logger.LogInformation("Current vertical louver direction: {Value}", value);
                    return value;
                }
            }
        }

        _logger.LogWarning("af_vertical_direction property not found");
        return "0"; // Default to auto
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DeviceProperty>> GetAllDevicePropertiesAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_settings.DeviceDsn))
        {
            throw new InvalidOperationException("Device DSN must be configured");
        }

        var endpoint = string.Format(DevicePropertiesEndpoint, _settings.DeviceDsn);
        var response = await ExecuteRequestWithRetryAsync(
            () => _httpClient.GetAsync(endpoint, cancellationToken),
            cancellationToken);
        
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseContent);

        var properties = new List<DeviceProperty>();

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var propertyElement in doc.RootElement.EnumerateArray())
            {
                if (propertyElement.TryGetProperty("property", out var propObj) &&
                    propObj.TryGetProperty("name", out var nameElement))
                {
                    var name = nameElement.GetString() ?? "";
                    var value = "";

                    if (propObj.TryGetProperty("value", out var valueElement))
                    {
                        value = valueElement.ValueKind switch
                        {
                            JsonValueKind.String => valueElement.GetString() ?? "",
                            JsonValueKind.Number => valueElement.GetInt32().ToString(),
                            JsonValueKind.True => "1",
                            JsonValueKind.False => "0",
                            _ => ""
                        };
                    }

                    properties.Add(new DeviceProperty { Name = name, Value = value });
                }
            }
        }

        _logger.LogInformation("Retrieved {Count} device properties", properties.Count);
        return properties;
    }

    /// <inheritdoc />
    public async Task<double> GetTemperatureAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_settings.DeviceDsn))
        {
            throw new InvalidOperationException("Device DSN must be configured");
        }

        // Get device properties to find temperature setting
        var endpoint = string.Format(DevicePropertiesEndpoint, _settings.DeviceDsn);
        var response = await ExecuteRequestWithRetryAsync(
            () => _httpClient.GetAsync(endpoint, cancellationToken),
            cancellationToken);
        
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseContent);

        // Parse the properties array to find temperature setting
        // Common property names: af_temp_setting, temp_setting, set_temp, target_temp
        var temperaturePropertyNames = new[] { "af_temp_setting", "temp_setting", "set_temp", "target_temp" };

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var propertyElement in doc.RootElement.EnumerateArray())
            {
                if (propertyElement.TryGetProperty("property", out var propObj) &&
                    propObj.TryGetProperty("name", out var nameElement))
                {
                    var propertyName = nameElement.GetString() ?? "";
                    if (temperaturePropertyNames.Contains(propertyName) &&
                        propObj.TryGetProperty("value", out var valueElement))
                    {
                        var temperature = valueElement.ValueKind switch
                        {
                            JsonValueKind.String when double.TryParse(valueElement.GetString(), out var strTemp) => strTemp,
                            JsonValueKind.Number => valueElement.GetDouble(),
                            _ => 0.0
                        };
                        
                        _logger.LogInformation("Current temperature setting ({PropertyName}): {Temperature}°C", propertyName, temperature);
                        return temperature;
                    }
                }
            }
        }

        _logger.LogWarning("Temperature setting property not found");
        throw new InvalidOperationException("Temperature setting property not found");
    }

    /// <inheritdoc />
    public async Task SetTemperatureAsync(double temperatureCelsius, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_settings.DeviceDsn))
        {
            throw new InvalidOperationException("Device DSN must be configured");
        }

        // Validate temperature range (typical range for heat pumps)
        if (temperatureCelsius < MinTemperatureCelsius || temperatureCelsius > MaxTemperatureCelsius)
        {
            throw new ArgumentOutOfRangeException(nameof(temperatureCelsius), 
                $"Temperature must be between {MinTemperatureCelsius}°C and {MaxTemperatureCelsius}°C");
        }

        _logger.LogInformation("Setting temperature to {Temperature}°C for device {DeviceDsn}", 
            temperatureCelsius, _settings.DeviceDsn);

        // Try the most common temperature property name first (af_temp_setting)
        var propertyName = "af_temp_setting";
        var endpoint = string.Format(SetPropertyEndpoint, _settings.DeviceDsn, propertyName);
        var request = new
        {
            datapoint = new
            {
                value = temperatureCelsius
            }
        };

        try
        {
            var response = await ExecuteRequestWithRetryAsync(
                () => _httpClient.PostAsJsonAsync(endpoint, request, _jsonOptions, cancellationToken),
                cancellationToken);
            
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Temperature set successfully to {Temperature}°C", temperatureCelsius);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to set temperature using property {PropertyName}, will attempt with alternate property names", propertyName);
            
            // Try alternative property names
            var alternateNames = new[] { "temp_setting", "set_temp", "target_temp" };
            foreach (var altName in alternateNames)
            {
                try
                {
                    var altEndpoint = string.Format(SetPropertyEndpoint, _settings.DeviceDsn, altName);
                    var altResponse = await ExecuteRequestWithRetryAsync(
                        () => _httpClient.PostAsJsonAsync(altEndpoint, request, _jsonOptions, cancellationToken),
                        cancellationToken);
                    
                    altResponse.EnsureSuccessStatusCode();
                    _logger.LogInformation("Temperature set successfully to {Temperature}°C using property {PropertyName}", temperatureCelsius, altName);
                    return;
                }
                catch (HttpRequestException)
                {
                    _logger.LogDebug("Failed to set temperature using property {PropertyName}", altName);
                }
            }
            
            throw new InvalidOperationException("Unable to set temperature - no valid temperature property found", ex);
        }
    }
}