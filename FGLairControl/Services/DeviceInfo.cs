namespace FGLairControl.Services;

/// <summary>
/// Information about a FGLair device
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// The device ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// The device serial number/DSN
    /// </summary>
    public string Dsn { get; set; } = string.Empty;

    /// <summary>
    /// The device name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The device model
    /// </summary>
    public string Model { get; set; } = string.Empty;
}