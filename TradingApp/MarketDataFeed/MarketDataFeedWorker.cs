using Microsoft.Extensions.Options;
using TradingApp.Services.PaperTrading;

namespace TradingApp.MarketDataFeed;

/// <summary>
/// Hosted background service that drives the market data feed and dispatches
/// each tick to <see cref="IMarketQuoteProcessor"/>.
/// Uses <see cref="IServiceScopeFactory"/> because <see cref="IMarketQuoteProcessor"/>
/// is scoped (depends on <c>DbContext</c>).
/// </summary>
public sealed class MarketDataFeedWorker(
    IMarketDataFeed feed,
    IServiceScopeFactory scopeFactory,
    IOptions<MarketDataFeedSettings> options,
    ILogger<MarketDataFeedWorker> logger) : BackgroundService
{
    private readonly MarketDataFeedSettings _settings = options.Value;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.AutoStart)
        {
            logger.LogInformation("MarketDataFeedWorker is disabled via AutoStart=false.");
            return;
        }

        if (_settings.Symbols.Count == 0)
        {
            logger.LogWarning("MarketDataFeedWorker: no symbols configured, worker will not start.");
            return;
        }

        logger.LogInformation(
            "MarketDataFeedWorker starting with provider '{Provider}', {Count} symbol(s), interval {Interval} ms.",
            feed.ProviderName,
            _settings.Symbols.Count,
            _settings.IntervalMs);

        var intervalDelay = TimeSpan.FromMilliseconds(_settings.IntervalMs);

        await foreach (var tick in feed.StreamAsync(_settings.Symbols, stoppingToken)
            .ConfigureAwait(false))
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessTickWithRetryAsync(tick, stoppingToken);
            await Task.Delay(intervalDelay, stoppingToken).ConfigureAwait(false);
        }

        logger.LogInformation("MarketDataFeedWorker stopped.");
    }

    private async Task ProcessTickWithRetryAsync(MarketQuoteTick tick, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IMarketQuoteProcessor>();
            var result = await processor.ProcessQuoteAsync(tick, stoppingToken);

            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Quote processing failed for {Symbol}.{Exchange}: [{Code}] {Message}",
                    tick.Symbol, tick.Exchange, result.ErrorCode, result.ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown — do not log as error
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error processing tick {Symbol}.{Exchange} @ {Price}.",
                tick.Symbol, tick.Exchange, tick.Price);
        }
    }
}
