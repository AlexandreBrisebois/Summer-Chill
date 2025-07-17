using FGLairControl.Services.Models;

namespace FGLairControl.Services;

/// <summary>
/// Interface for the FGLair API client
/// </summary>
public interface IFGLairClient
{
    /// <summary>
    /// Authenticates with the FGLair API using credentials
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task LoginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of devices associated with the account
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A collection of device information</returns>
    Task<IEnumerable<DeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a louver command to the FGLair air conditioner with a specific position
    /// </summary>
    /// <param name="position">The louver position to set (0=Auto, 1-8=Positions)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SendLouverCommandAsync(string position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current louver position from the FGLair air conditioner
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The louver direction value</returns>
    Task<string> GetLouverPositionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available device properties for debugging purposes
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A collection of device properties</returns>
    Task<IEnumerable<DeviceProperty>> GetAllDevicePropertiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current temperature setting from the FGLair air conditioner
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The temperature setting in Celsius</returns>
    Task<double> GetTemperatureAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the temperature on the FGLair air conditioner
    /// </summary>
    /// <param name="temperatureCelsius">The temperature to set in Celsius</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SetTemperatureAsync(double temperatureCelsius, CancellationToken cancellationToken = default);
}