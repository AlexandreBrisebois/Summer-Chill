using System.Text.Json.Serialization;

namespace FGLairControl.Services.Models;

/// <summary>
/// Request model for login
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// The user credentials
    /// </summary>
    [JsonPropertyName("user")]
    public UserCredentials User { get; set; } = new();
}

/// <summary>
/// User credentials for login
/// </summary>
public class UserCredentials
{
    /// <summary>
    /// The email/username
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The password
    /// </summary>
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// The application details
    /// </summary>
    [JsonPropertyName("application")]
    public AppDetails Application { get; set; } = new();
}

/// <summary>
/// Application details for login
/// </summary>
public class AppDetails
{
    /// <summary>
    /// The application ID
    /// </summary>
    [JsonPropertyName("app_id")]
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// The application secret
    /// </summary>
    [JsonPropertyName("app_secret")]
    public string AppSecret { get; set; } = string.Empty;
}

/// <summary>
/// Response model for login
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// The access token
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// The refresh token
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// The time in seconds until the token expires
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}