using Microsoft.EntityFrameworkCore;
using TradingApp.Data;
using TradingApp.DTOs.Setup;
using TradingApp.Entities.Enums;
using TradingApp.Services.PaperTrading;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Scans open paper positions against cached quotes and emits sell alerts when SL/TP is hit.
/// </summary>
public sealed class PositionExitScanner(
    ApplicationDbContext dbContext,
    IMarketQuoteStore quoteStore,
    IPositionRiskMonitor riskMonitor,
    IExitAlertTracker alertTracker,
    IExitSignalNotifier notifier,
    ILogger<PositionExitScanner> logger) : IPositionExitScanner
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ExitSignalAlertDto>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var positions = await dbContext.Positions
            .AsNoTracking()
            .Include(p => p.Portfolio)
            .Where(p => p.Status == PositionStatus.Open && p.Portfolio.PaperTradeAccountId != null)
            .ToListAsync(cancellationToken);

        alertTracker.PruneClosedPositions(positions.Select(p => p.Id).ToList());

        if (positions.Count == 0)
        {
            return Array.Empty<ExitSignalAlertDto>();
        }

        var alerts = new List<ExitSignalAlertDto>();

        foreach (var position in positions)
        {
            if (!quoteStore.TryGetPrice(position.Symbol, position.Exchange, out var marketPrice))
            {
                logger.LogDebug(
                    "Exit scan skipped for {Symbol}.{Exchange} — no cached quote.",
                    position.Symbol,
                    position.Exchange);
                continue;
            }

            var accountId = position.Portfolio.PaperTradeAccountId!.Value;
            var exits = riskMonitor.GetTriggeredExits(position, accountId, marketPrice);
            foreach (var exit in exits)
            {
                var reason = exit.Reason.ToString();
                if (!alertTracker.TryRegister(exit.PositionId, reason))
                {
                    continue;
                }

                var alert = ToAlert(exit, position, marketPrice);
                alerts.Add(alert);
                await notifier.NotifyExitSignalAsync(alert, cancellationToken);
                logger.LogInformation(
                    "Sell alert {Reason} for {Symbol} @ {Price} (trigger {Trigger}).",
                    reason,
                    exit.Symbol,
                    marketPrice,
                    exit.TriggerPrice);
            }
        }

        return alerts;
    }

    private static ExitSignalAlertDto ToAlert(
        RiskExitSignal exit,
        Entities.Position position,
        decimal marketPrice)
    {
        var triggerPrice = exit.Reason == RiskExitReason.StopLoss
            ? position.StopLossPrice ?? marketPrice
            : position.TakeProfitPrice ?? marketPrice;

        var message = exit.Reason == RiskExitReason.StopLoss
            ? $"{exit.Symbol}: Kurs {marketPrice:F5} hat Stop-Loss {triggerPrice:F5} erreicht — verkaufen."
            : $"{exit.Symbol}: Kurs {marketPrice:F5} hat Take-Profit {triggerPrice:F5} erreicht — Gewinn mitnehmen.";

        return new ExitSignalAlertDto(
            exit.PositionId,
            exit.PaperTradeAccountId,
            exit.Symbol,
            exit.Exchange,
            exit.Reason.ToString(),
            marketPrice,
            triggerPrice,
            message);
    }
}
