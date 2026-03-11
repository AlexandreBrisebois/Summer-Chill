using FGLairControl.Services;
using Microsoft.Extensions.Options;

namespace FGLairControl;

public class Worker : BackgroundService
{
    private const int RetryDelayMinutes = 1;

    private readonly ILogger<Worker> _logger;
    private readonly IFGLairClient _fgLairClient;
    private readonly int _intervalMinutes;

    private readonly IReadOnlyList<string> _louverPositions;
    private int _currentPositionIndex = 0;

    public Worker(
        ILogger<Worker> logger,
        IFGLairClient fgLairClient,
        IOptions<FGLairSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fgLairClient = fgLairClient ?? throw new ArgumentNullException(nameof(fgLairClient));
        ArgumentNullException.ThrowIfNull(settings);

        var options = settings.Value;
        _intervalMinutes = options.Interval;

        var raw = string.IsNullOrWhiteSpace(options.LouverPositions) ? "7,8" : options.LouverPositions;
        _louverPositions = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (_louverPositions.Count == 0)
            throw new InvalidOperationException("No louver positions configured.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FGLair Control Worker started at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("Louver will cycle between positions {Positions} every {Interval} minutes",
            string.Join(", ", _louverPositions), _intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _fgLairClient.LoginAsync(stoppingToken);
                _logger.LogInformation("Authenticated with FGLair API");

                await CheckCurrentLouverPositionAsync(stoppingToken);
                await SendCyclingLouverCommandAsync(stoppingToken);
                await RunPeriodicCommandsAsync(stoppingToken);

                // RunPeriodicCommandsAsync only exits on clean cancellation
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker encountered an error, retrying in {Delay} minute(s)", RetryDelayMinutes);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(RetryDelayMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task CheckCurrentLouverPositionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var currentPosition = await _fgLairClient.GetLouverPositionAsync(cancellationToken);
            _logger.LogInformation("Current louver position: {Position} ({Description})",
                currentPosition, GetPositionDescription(currentPosition));
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
            var position = _louverPositions[_currentPositionIndex];
            await _fgLairClient.SendLouverCommandAsync(position, cancellationToken);

            _logger.LogInformation("Louver command sent. Position: {Position} ({Description})",
                position, GetPositionDescription(position));

            _currentPositionIndex = (_currentPositionIndex + 1) % _louverPositions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send louver command");
        }
    }

    private async Task RunPeriodicCommandsAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting periodic louver cycling every {Interval} minutes between positions {Positions}",
            _intervalMinutes, string.Join(", ", _louverPositions));

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_intervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckCurrentLouverPositionAsync(stoppingToken);
                await SendCyclingLouverCommandAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic louver cycling");
            }
        }
    }

    private static string GetPositionDescription(string position) => position switch
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
        _ => "Unknown"
    };

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FGLair Control Worker stopping at: {time}", DateTimeOffset.Now);
        await base.StopAsync(cancellationToken);
    }
}
