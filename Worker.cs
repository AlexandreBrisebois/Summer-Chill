using FGLairControl.Services;

namespace FGLairControl;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IFGLairClient _fgLairClient;
    private readonly int _intervalMinutes;

    // Use a list of positions for flexibility
    private readonly List<string> _louverPositions;
    private int _currentPositionIndex = 0;

    public Worker(
        ILogger<Worker> logger,
        IFGLairClient fgLairClient,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fgLairClient = fgLairClient ?? throw new ArgumentNullException(nameof(fgLairClient));

        // Read interval from configuration
        _intervalMinutes = configuration.GetSection("FGLair:Interval").Get<int?>() ?? 20;

        // Read louver positions from configuration (comma-separated string)
        var positions = configuration.GetSection("FGLair:LouverPositions").Get<string>() ?? "7,8";
        _louverPositions = positions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (_louverPositions.Count == 0)
            throw new InvalidOperationException("No louver positions configured.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FGLair Control Worker started at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("Louver will cycle between positions 7 and 8 every {Interval} minutes", _intervalMinutes);

        try
        {
            // Authenticate with the API
            await _fgLairClient.LoginAsync(stoppingToken);
            _logger.LogInformation("Successfully authenticated with FGLair API");

            // Check current louver position
            await CheckCurrentLouverPositionAsync(stoppingToken);

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
        
        _logger.LogInformation("Starting periodic louver cycling every {Interval} minutes between positions 7 and 8", _intervalMinutes);
        
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Check current position
                await CheckCurrentLouverPositionAsync(stoppingToken);
                
                // Send cycling command
                await SendCyclingLouverCommandAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic louver cycling execution");
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
