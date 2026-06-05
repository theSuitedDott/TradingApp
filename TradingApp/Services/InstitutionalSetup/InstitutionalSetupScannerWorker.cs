using Microsoft.Extensions.Options;
using TradingApp.Configuration;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Hosted background service that periodically scans the configured symbol for the
/// institutional setup and emits a trade opportunity when all conditions are met.
/// The scanner is singleton and does not touch the database, so no scope is required.
/// </summary>
public sealed class InstitutionalSetupScannerWorker(
    IInstitutionalSetupScanner scanner,
    IOptions<InstitutionalSetupSettings> options,
    ILogger<InstitutionalSetupScannerWorker> logger) : BackgroundService
{
    private readonly InstitutionalSetupSettings _settings = options.Value;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("InstitutionalSetupScannerWorker is disabled via Enabled=false.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, _settings.ScanIntervalSeconds));
        logger.LogInformation(
            "InstitutionalSetupScannerWorker starting for {Symbol} ({Exchange}), interval {Interval}s.",
            _settings.Symbol,
            _settings.Exchange,
            interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await scanner.ScanAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Institutional setup scan failed.");
            }
        }
        while (await WaitAsync(timer, stoppingToken));

        logger.LogInformation("InstitutionalSetupScannerWorker stopped.");
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
