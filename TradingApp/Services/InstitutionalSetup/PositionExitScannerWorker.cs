using Microsoft.Extensions.Options;
using TradingApp.Configuration;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Background worker that checks open positions for sell signals every minute.
/// </summary>
public sealed class PositionExitScannerWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<InstitutionalSetupSettings> options,
    ILogger<PositionExitScannerWorker> logger) : BackgroundService
{
    private readonly InstitutionalSetupSettings _settings = options.Value;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("PositionExitScannerWorker is disabled via Enabled=false.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(10, _settings.ExitScanIntervalSeconds));
        logger.LogInformation(
            "PositionExitScannerWorker starting — exit check every {Interval}s.",
            interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var scanner = scope.ServiceProvider.GetRequiredService<IPositionExitScanner>();
                var alerts = await scanner.ScanAsync(stoppingToken);
                if (alerts.Count > 0)
                {
                    logger.LogInformation("Exit scan emitted {Count} sell alert(s).", alerts.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Position exit scan failed.");
            }
        }
        while (await WaitAsync(timer, stoppingToken));

        logger.LogInformation("PositionExitScannerWorker stopped.");
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
