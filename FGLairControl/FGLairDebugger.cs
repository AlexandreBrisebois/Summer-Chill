using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FGLairControl;

/// <summary>
/// Simple debug utility to test FGLair API endpoints
/// </summary>
public static class FGLairDebugger
{
    private static readonly HttpClient _httpClient = new();
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Test FGLair API authentication and device property access
    /// </summary>
    public static async Task DebugApiAsync()
    {
        try
        {
            Console.WriteLine("=== FGLair API Debug Tool ===\n");
            
            // Configure HttpClient based on rest.http examples
            _httpClient.BaseAddress = new Uri("https://user-field.aylanetworks.com");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FGLAir", "2.0"));
            _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");

            // Step 1: Authenticate
            Console.WriteLine("Step 1: Authenticating...");
            var accessToken = await AuthenticateAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("❌ Authentication failed");
                return;
            }
            Console.WriteLine($"✅ Authentication successful\n");

            // Step 2: Test device properties
            Console.WriteLine("Step 2: Testing device properties...");
            await TestDevicePropertiesAsync(accessToken, "AC000W002826905");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Debug failed: {ex.Message}");
        }
    }

    private static async Task<string> AuthenticateAsync()
    {
        try
        {
            // Use the exact login request from rest.http
            var loginRequest = new
            {
                user = new
                {
                    email = "brisebois@outlook.com",
                    password = "dt0Iamr6T0",
                    application = new
                    {
                        app_id = FGLairSettings.AppId,
                        app_secret = FGLairSettings.AppSecret
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync("/users/sign_in.json", loginRequest, _jsonOptions);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            
            if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
            {
                var token = tokenElement.GetString() ?? "";
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("auth_token", token);
                return token;
            }

            return "";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authentication error: {ex.Message}");
            return "";
        }
    }

    private static async Task TestDevicePropertiesAsync(string accessToken, string deviceDsn)
    {
        try
        {
            var endpoint = $"/apiv1/dsns/{deviceDsn}/properties";
            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);
                
                Console.WriteLine($"✅ Device properties retrieved for {deviceDsn}");
                
                // Look for af_vertical_direction specifically
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var propertyElement in doc.RootElement.EnumerateArray())
                    {
                        if (propertyElement.TryGetProperty("property", out var propObj) &&
                            propObj.TryGetProperty("name", out var nameElement) &&
                            nameElement.GetString() == "af_vertical_direction")
                        {
                            if (propObj.TryGetProperty("value", out var valueElement))
                            {
                                Console.WriteLine($"Current af_vertical_direction: {valueElement}");
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"❌ Failed to get device properties: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error testing device properties: {ex.Message}");
        }
    }
}
