using FGLairControl.Services;
using Microsoft.Extensions.Options;

namespace FGLairControl;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IFGLairClient _fgLairClient;
    private readonly FGLairSettings _settings;

    public Worker(
        ILogger<Worker> logger,
        IFGLairClient fgLairClient,
        IOptions<FGLairSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fgLairClient = fgLairClient ?? throw new ArgumentNullException(nameof(fgLairClient));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FGLair Control Worker started at: {time}", DateTimeOffset.Now);

        try
        {
            // Authenticate with the API
            await _fgLairClient.LoginAsync(stoppingToken);
            _logger.LogInformation("Successfully authenticated with FGLair API");

            // Check current louver position
            await CheckCurrentLouverPositionAsync(stoppingToken);

            // Send initial louver command
            await SendLouverCommandAsync(stoppingToken);

            // Setup periodic command sending
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

    private async Task SendLouverCommandAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _fgLairClient.SendLouverCommandAsync(cancellationToken);
            
            var targetDescription = GetPositionDescription(_settings.LouverPosition);
            _logger.LogInformation("Louver command sent. Target position: {Position} ({Description})", 
                _settings.LouverPosition, targetDescription);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] Louver Command Sent: {_settings.LouverPosition} ({targetDescription})");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send louver command");
        }
    }

    private async Task RunPeriodicCommandsAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.CommandIntervalMinutes));
        
        _logger.LogInformation("Starting periodic commands every {Interval} minutes", _settings.CommandIntervalMinutes);
        
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Check current position
                await CheckCurrentLouverPositionAsync(stoppingToken);
                
                // Send command
                await SendLouverCommandAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic command execution");
            }
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
        "7" => "Position 7",
        "8" => "Position 8",
        _ => "Unknown Position"
    };

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FGLair Control Worker stopping at: {time}", DateTimeOffset.Now);
        await base.StopAsync(cancellationToken);
    }
}
