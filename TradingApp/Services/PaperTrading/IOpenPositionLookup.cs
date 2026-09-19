namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Checks whether open paper positions exist for a symbol (setup notification gating).
/// </summary>
public interface IOpenPositionLookup
{
    /// <summary>Returns true when at least one open position exists for the instrument.</summary>
    Task<bool> HasOpenPositionAsync(string symbol, string exchange, CancellationToken cancellationToken = default);
}
