using System.Text.Json.Serialization;

namespace FGLairControl.Services.Models;

/// <summary>
/// Device information model
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// The device ID (legacy - use Dsn instead)
    /// </summary>
    [Obsolete("Use DeviceDsn (Dsn property) instead for all device operations")]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// The DSN (Device Serial Number)
    /// </summary>
    public string Dsn { get; set; } = string.Empty;

    /// <summary>
    /// The name of the device
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The model of the device
    /// </summary>
    public string Model { get; set; } = string.Empty;
}

/// <summary>
/// Response model for device listing
/// </summary>
public class DeviceResponse
{
    /// <summary>
    /// The list of devices
    /// </summary>
    [JsonPropertyName("devices")]
    public List<DeviceWrapper> Devices { get; set; } = new();
}

/// <summary>
/// Wrapper for device details in the API response
/// </summary>
public class DeviceWrapper
{
    /// <summary>
    /// The device details
    /// </summary>
    [JsonPropertyName("device")]
    public DeviceDetails DeviceDetails { get; set; } = new();
}

/// <summary>
/// Device details from the API
/// </summary>
public class DeviceDetails
{
    /// <summary>
    /// The device ID (legacy - use Dsn instead)
    /// </summary>
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// The DSN (Device Serial Number)
    /// </summary>
    [JsonPropertyName("dsn")]
    public string Dsn { get; set; } = string.Empty;

    /// <summary>
    /// The product name
    /// </summary>
    [JsonPropertyName("product_name")]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// The model of the device
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
}

/// <summary>
/// Request model for sending datapoint commands to the device
/// </summary>
public class DatapointRequest
{
    /// <summary>
    /// The datapoint data
    /// </summary>
    [JsonPropertyName("datapoint")]
    public DatapointData Datapoint { get; set; } = new();
}

/// <summary>
/// Datapoint data for device commands
/// </summary>
public class DatapointData
{
    /// <summary>
    /// The value to set
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

/// <summary>
/// Error response model
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// The error code or message
    /// </summary>
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Detailed error description
    /// </summary>
    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}

/// <summary>
/// Model for device property information
/// </summary>
public class DeviceProperty
{
    /// <summary>
    /// The name of the property
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The current value of the property
    /// </summary>
    public string Value { get; set; } = string.Empty;
}