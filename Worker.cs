using FGLairControl.Services;
using Microsoft.Extensions.Options;

namespace FGLairControl;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IFGLairClient _fgLairClient;
    private readonly IWeatherService _weatherService;
    private readonly FGLairSettings _settings;
    private readonly int _intervalMinutes;
    private readonly bool _enableWeatherControl;

    // Use a list of positions for flexibility
    private readonly List<string> _louverPositions;
    private int _currentPositionIndex = 0;

    public Worker(
        ILogger<Worker> logger,
        IFGLairClient fgLairClient,
        IWeatherService weatherService,
        IOptions<FGLairSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fgLairClient = fgLairClient ?? throw new ArgumentNullException(nameof(fgLairClient));
        _weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

        // Read interval from settings
        _intervalMinutes = _settings.Interval;

        // Read weather control setting
        _enableWeatherControl = _settings.EnableWeatherControl;

        // Read louver positions from settings (comma-separated string)
        var positions = _settings.LouverPositions;
        if (positions == null)
            throw new InvalidOperationException("Louver positions are not configured.");
        _louverPositions = positions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (_louverPositions.Count == 0)
            throw new InvalidOperationException("No louver positions configured.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FGLair Control Worker started at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("Louver will cycle between positions {Positions} every {Interval} minutes", string.Join(", ", _louverPositions), _intervalMinutes);
        
        if (_enableWeatherControl)
        {
            _logger.LogInformation("Weather-based temperature control is enabled");
        }

        try
        {
            // Authenticate with the API
            await _fgLairClient.LoginAsync(stoppingToken);
            _logger.LogInformation("Successfully authenticated with FGLair API");

            // Check current louver position
            await CheckCurrentLouverPositionAsync(stoppingToken);

            // Check current temperature setting
            await CheckCurrentTemperatureAsync(stoppingToken);

            // Perform initial weather check and temperature adjustment if enabled
            if (_enableWeatherControl)
            {
                await CheckWeatherAndAdjustTemperatureAsync(stoppingToken);
            }

            // Send initial louver command (start with position 7)
            await SendCyclingLouverCommandAsync(stoppingToken);

            // Setup periodic command sending every 20 minutes
            await RunPeriodicCommandsAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in worker execution");
        }
    }

    private async Task CheckCurrentLouverPositionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var currentPosition = await _fgLairClient.GetLouverPositionAsync(cancellationToken);
            var positionDescription = GetPositionDescription(currentPosition);
            
            _logger.LogInformation("Current louver position: {Position} ({Description})", 
                currentPosition, positionDescription);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] Current Louver Position: {currentPosition} ({positionDescription})");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check current louver position");
        }
    }

    private async Task SendCyclingLouverCommandAsync(CancellationToken cancellationToken)
    {
        try
        {
            var currentPosition = _louverPositions[_currentPositionIndex];
            await _fgLairClient.SendLouverCommandAsync(currentPosition, cancellationToken);
            
            var targetDescription = GetPositionDescription(currentPosition);
            _logger.LogInformation("Cycling louver command sent. Position: {Position} ({Description})", 
                currentPosition, targetDescription);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] Cycling Louver Command Sent: {currentPosition} ({targetDescription})");
            Console.ResetColor();

            // Move to next position for the next cycle
            _currentPositionIndex = (_currentPositionIndex + 1) % _louverPositions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send cycling louver command");
        }
    }

    private async Task RunPeriodicCommandsAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_intervalMinutes)); // Use configured interval
        
        _logger.LogInformation("Starting periodic louver cycling every {Interval} minutes between positions {PositionA} and {PositionB}", 
            _intervalMinutes, _louverPositions[0], _louverPositions[1]);
        
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Check current position
                await CheckCurrentLouverPositionAsync(stoppingToken);
                
                // Check weather and adjust temperature if enabled
                if (_enableWeatherControl)
                {
                    await CheckWeatherAndAdjustTemperatureAsync(stoppingToken);
                }
                
                // Send cycling command
                await SendCyclingLouverCommandAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic louver cycling execution");
            }
        }
    }

    private async Task CheckCurrentTemperatureAsync(CancellationToken cancellationToken)
    {
        try
        {
            var currentTemp = await _fgLairClient.GetTemperatureAsync(cancellationToken);
            
            _logger.LogInformation("Current temperature setting: {Temperature}°C at {Timestamp}", currentTemp, DateTimeOffset.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check current temperature setting");
        }
    }

    private async Task CheckWeatherAndAdjustTemperatureAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Get current weather
            var weather = await _weatherService.GetCurrentWeatherAsync(cancellationToken);
            var outsideTemp = weather.TemperatureCelsius;

            _logger.LogInformation("Outside Temperature: {OutsideTemp}°C at {Timestamp}", outsideTemp, DateTimeOffset.Now);

            // Apply temperature control logic when outside temp >= 30°C
            if (outsideTemp >= 30.0)
            {
                // Heat pump should be set to at most 10°C cooler than outside temperature
                var maxHeatPumpTemp = outsideTemp - 10.0;
                
                // Get current heat pump temperature setting
                var currentTemp = await _fgLairClient.GetTemperatureAsync(cancellationToken);
                
                if (currentTemp < maxHeatPumpTemp)
                {
                    // Current setting is too low (too cold), adjust upward
                    await _fgLairClient.SetTemperatureAsync(maxHeatPumpTemp, cancellationToken);
                    
                    _logger.LogInformation("Temperature adjusted from {OldTemp}°C to {NewTemp}°C due to high outside temperature ({OutsideTemp}°C)", 
                        currentTemp, maxHeatPumpTemp, outsideTemp);

                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] Temperature Adjusted: {currentTemp}°C → {maxHeatPumpTemp}°C (Outside: {outsideTemp}°C)");
                    Console.ResetColor();
                }
                else
                {
                    _logger.LogInformation("Temperature setting {CurrentTemp}°C is appropriate for outside temperature {OutsideTemp}°C", 
                        currentTemp, outsideTemp);
                }
            }
            else
            {
                _logger.LogInformation("Outside temperature {OutsideTemp}°C is below 30°C threshold, no temperature adjustment needed", 
                    outsideTemp);
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Weather city must be configured"))
        {
            _logger.LogWarning("Weather city not configured - skipping weather-based temperature control. Set WeatherCity in configuration.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("network") || ex.Message.Contains("Unable to retrieve weather"))
        {
            _logger.LogWarning("Weather service unavailable - skipping temperature adjustment this cycle. Error: {Error}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check weather and adjust temperature");
        }
    }

    private string GetPositionDescription(string position) => position switch
    {
        "0" => "Auto",
        "1" => "Position 1 (highest)",
        "2" => "Position 2",
        "3" => "Position 3",
        "4" => "Position 4",
        "5" => "Position 5 (lowest/down)",
        "6" => "Position 6",
        "7" => "Position 7 (cycling position A)",
        "8" => "Position 8 (cycling position B)",
        _ => "Unknown Position"
    };

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FGLair Control Worker stopping at: {time}", DateTimeOffset.Now);
        await base.StopAsync(cancellationToken);
    }
}
