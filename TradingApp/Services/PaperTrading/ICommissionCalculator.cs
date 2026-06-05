namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Calculates trading commissions for paper fills.
/// </summary>
public interface ICommissionCalculator
{
    /// <summary>
    /// Calculates commission for a notional value.
    /// </summary>
    /// <param name="notional">Price multiplied by quantity.</param>
    /// <returns>Commission amount.</returns>
    decimal Calculate(decimal notional);
}
