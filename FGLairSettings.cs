namespace FGLairControl;

/// <summary>
/// Settings for the FGLair API
/// </summary>
public class FGLairSettings
{
    /// <summary>
    /// The base URL for the FGLair API (readonly)
    /// </summary>
    public const string BaseUrl = "https://user-field.aylanetworks.com";

    /// <summary>
    /// Application ID for the FGLair API (readonly)
    /// </summary>
    public const string AppId = "CJIOSP-id";

    /// <summary>
    /// Application secret for the FGLair API (readonly)
    /// </summary>
    public const string AppSecret = "CJIOSP-Vb8MQL_lFiYQ7DKjN0eCFXznKZE";

    /// <summary>
    /// Username (email) for authentication
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// The DSN (Device Serial Number) of the air conditioner
    /// </summary>
    public string DeviceDsn { get; set; } = string.Empty;

    /// <summary>
    /// The desired louver position (0=Auto, 1-8=Positions)
    /// </summary>
    public string LouverPosition { get; set; } = "8";

    /// <summary>
    /// Command interval in minutes
    /// </summary>
    public int CommandIntervalMinutes { get; set; } = 30;
}