namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Recalculates portfolio equity and position unrealized PnL from market prices.
/// </summary>
public interface IPortfolioValuationService
{
    /// <summary>
    /// Updates unrealized PnL and total equity for a portfolio.
    /// </summary>
    Task RecalculateAsync(Guid portfolioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates all portfolios with open positions for the given instrument.
    /// </summary>
    Task RecalculateByInstrumentAsync(
        string symbol,
        string exchange,
        decimal currentPrice,
        CancellationToken cancellationToken = default);
}
